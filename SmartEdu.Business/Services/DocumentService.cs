using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using SmartEdu.Shared.Helpers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using EntityDocument = SmartEdu.Shared.Entities.Document;

namespace SmartEdu.Business.Services;

public class DocumentService : IDocumentService
{
    private readonly IRepository<EntityDocument> _docRepo;
    private readonly IRepository<DocumentChunk> _chunkRepo;
    private readonly IRepository<StudentSubject> _studentSubjectRepo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _uow;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRepository<EmbeddingSet> _embeddingSetRepo;
    private readonly IChunkingConfigService _chunkingConfigService;
    private readonly IRepository<LecturerSubject> _lecturerSubjectRepo;
    private readonly IRepository<Subject> _subjectRepo;
    private readonly IRealtimeNotifier _realtime;
    private readonly IUploadConfigService _uploadConfigService;
    private const int MaxChunksForDuplicateCheck = 10;
    private readonly AppDbContext _context;

    public DocumentService(
    IRepository<EntityDocument> docRepo,
    IRepository<DocumentChunk> chunkRepo,
    IRepository<StudentSubject> studentSubjectRepo,
    IHttpClientFactory httpFactory,
    IConfiguration configuration,
    IUnitOfWork uow,
    IServiceScopeFactory scopeFactory,
    IRepository<EmbeddingSet> embeddingSetRepo,
    IChunkingConfigService chunkingConfigService,
    IRepository<LecturerSubject> lecturerSubjectRepo,
    IRealtimeNotifier realtime,
    IRepository<Subject> subjectRepo,
    IUploadConfigService uploadConfigService,
    AppDbContext context)
    {
        _docRepo = docRepo;
        _chunkRepo = chunkRepo;
        _studentSubjectRepo = studentSubjectRepo;
        _httpFactory = httpFactory;
        _configuration = configuration;
        _uow = uow;
        _scopeFactory = scopeFactory;
        _embeddingSetRepo = embeddingSetRepo;
        _chunkingConfigService = chunkingConfigService;
        _lecturerSubjectRepo = lecturerSubjectRepo;
        _realtime = realtime;
        _subjectRepo = subjectRepo;
        _uploadConfigService = uploadConfigService;
        _context = context;
    }
    public async Task<DocumentChunkDetailDto?> GetChunkDetailAsync(int chunkId)
    {
        var chunks = await _chunkRepo.GetAllWithIncludeAsync(c => c.Id == chunkId, c => c.EmbeddingSet, c => c.EmbeddingSet.Documents);
        var chunk = chunks.FirstOrDefault();
        if (chunk == null) return null;

        var dto = new DocumentChunkDetailDto
        {
            ChunkId = chunk.Id,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content,
            EmbeddingSetId = chunk.EmbeddingSetId,
            SourceLocation = chunk.SourceLocation,
            Documents = chunk.EmbeddingSet.Documents
                        .Where(d => !d.IsDeleted)
                        .Select(d => new DocumentShortDto { Id = d.Id, Title = d.Title, Status = (int)d.Status, CreatedAt = d.CreatedAt })
                        .ToList()
        };

        return dto;
    }

    public async Task<int?> GetChunkIndexByIdAsync(int chunkId)
    {
        var chunk = await _chunkRepo.GetByIdAsync(chunkId);
        return chunk?.ChunkIndex;
    }

    public async Task<DocumentSourcePanelDto?> GetChunksAroundCitationAsync(int documentId, int chunkId, int range = 10)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc?.EmbeddingSetId == null) return null;

        var chunk = await _chunkRepo.GetByIdAsync(chunkId);
        if (chunk == null) return null;

        int fromIndex = Math.Max(0, chunk.ChunkIndex - range);
        int toIndex = chunk.ChunkIndex + range;

        return await BuildSourcePanelAsync(doc, fromIndex, toIndex);
    }

    public async Task<DocumentSourcePanelDto?> GetChunksRangeAsync(int documentId, int fromIndex, int toIndex)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc?.EmbeddingSetId == null) return null;

        return await BuildSourcePanelAsync(doc, fromIndex, toIndex);
    }

    private async Task<DocumentSourcePanelDto> BuildSourcePanelAsync(EntityDocument doc, int fromIndex, int toIndex)
    {
        var totalChunks = (await _chunkRepo.GetAllAsync(c => c.EmbeddingSetId == doc.EmbeddingSetId!.Value)).Count();

        var pageChunks = await _chunkRepo.GetAllAsync(c =>
            c.EmbeddingSetId == doc.EmbeddingSetId!.Value &&
            c.ChunkIndex >= fromIndex &&
            c.ChunkIndex <= toIndex);

        var orderedChunks = pageChunks
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new DocumentPositionedChunkDto
            {
                ChunkId = c.Id,
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                SourceLocation = c.SourceLocation
            })
            .ToList();

        return new DocumentSourcePanelDto
        {
            DocumentId = doc.Id,
            DocumentTitle = doc.Title,
            TotalChunks = totalChunks,
            Chunks = orderedChunks
        };
    }

    public async Task<DocumentDto> CreateFromTempAsync(string tempFilePath, string originalFileName, string title, int subjectId, string fileHash, long fileSize, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadRoot = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadRoot);

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var savedName = $"{Guid.NewGuid()}{ext}";
        var savedPath = Path.Combine(uploadRoot, savedName);

        System.IO.File.Move(tempFilePath, savedPath);

        var doc = new EntityDocument
        {
            Title = title,
            FileName = originalFileName,
            FilePath = savedPath,
            FileType = ext.TrimStart('.'),
            FileSize = fileSize,
            FileHash = fileHash,
            SubjectId = subjectId,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _docRepo.AddAsync(doc);
        await _docRepo.SaveChangesAsync();

        return new DocumentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            FileName = doc.FileName,
            FileType = doc.FileType,
            FileSize = doc.FileSize,
            SubjectId = doc.SubjectId,
            Status = doc.Status,
            CreatedAt = doc.CreatedAt
        };
    }

    public async Task<IEnumerable<DocumentChunkDto>> GetChunksByDocumentIdAsync(int documentId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc?.EmbeddingSetId == null) return Enumerable.Empty<DocumentChunkDto>();

        var chunks = await _chunkRepo.GetAllAsync(c => c.EmbeddingSetId == doc.EmbeddingSetId.Value);
        var orderedChunks = chunks.OrderBy(c => c.ChunkIndex).ToList();
        var embeddingSet = await _embeddingSetRepo.GetByIdAsync(doc.EmbeddingSetId.Value);
        var config = embeddingSet == null
            ? null
            : await _context.ChunkingConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == embeddingSet.ChunkingConfigId);

        var result = new List<DocumentChunkDto>(orderedChunks.Count);
        for (var index = 0; index < orderedChunks.Count; index++)
        {
            var chunk = orderedChunks[index];
            var content = chunk.Content ?? string.Empty;
            var previousContent = index > 0 ? orderedChunks[index - 1].Content ?? string.Empty : string.Empty;

            result.Add(new DocumentChunkDto
            {
                ChunkId = chunk.Id,
                ChunkIndex = chunk.ChunkIndex,
                Content = content,
                DocumentTitle = doc.Title,
                SourceLocation = chunk.SourceLocation,
                CharacterCount = content.Length,
                WordCount = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                ConfiguredChunkSize = config?.ChunkSize ?? 0,
                ConfiguredOverlap = config?.ChunkOverlap ?? 0,
                ActualOverlap = index == 0 ? 0 : CalculateActualOverlap(previousContent, content),
                ChunkingStrategy = config?.Strategy.ToString() ?? "Unknown"
            });
        }

        return result;
    }

    private static int CalculateActualOverlap(string previousContent, string currentContent)
    {
        var maxLength = Math.Min(previousContent.Length, currentContent.Length);
        for (var length = maxLength; length > 0; length--)
        {
            if (previousContent.AsSpan(previousContent.Length - length, length)
                .SequenceEqual(currentContent.AsSpan(0, length)))
            {
                return length;
            }
        }

        return 0;
    }

    public async Task<IEnumerable<DocumentDto>> GetAllAsync(int? subjectId = null)
    {
        var docs = await _docRepo.GetAllWithIncludeAsync(
            d => (!subjectId.HasValue || d.SubjectId == subjectId.Value) && !d.IsDeleted,
            d => d.Subject
        );

        return docs.Select(d => new DocumentDto
        {
            Id = d.Id,
            Title = d.Title,
            FileName = d.FileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            SubjectId = d.SubjectId,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            SubjectName = d.Subject?.Name
        });
    }

    public async Task<DocumentDto?> GetByIdAsync(int id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null || doc.IsDeleted) return null;

        return new DocumentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            FileName = doc.FileName,
            FileType = doc.FileType,
            FileSize = doc.FileSize,
            SubjectId = doc.SubjectId,
            Status = doc.Status,
            CreatedAt = doc.CreatedAt
        };
    }

    public async Task<DocumentDto> UploadAsync(IFormFile file, string title, int subjectId, string webRootPath)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not ".pdf" and not ".docx" and not ".pptx")
            throw new InvalidOperationException("Chỉ hỗ trợ PDF, DOCX và PPTX.");

        var fileTypeKey = ext.TrimStart('.');
        var maxSizeMB = await _uploadConfigService.ResolveMaxFileSizeMBAsync(subjectId, fileTypeKey);
        var maxSizeBytes = (long)maxSizeMB * 1024 * 1024;

        if (file.Length > maxSizeBytes)
            throw new InvalidOperationException(
                $"File vượt quá kích thước cho phép ({maxSizeMB}MB). File của bạn: {(file.Length / 1024.0 / 1024.0):F1}MB.");

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadRoot = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadRoot);

        var savedName = $"{Guid.NewGuid()}{ext}";
        var savedPath = Path.Combine(uploadRoot, savedName);

        await using var stream = File.Create(savedPath);
        await file.CopyToAsync(stream);
        stream.Position = 0;

        string fileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(stream);
            fileHash = Convert.ToHexString(hashBytes);
        }

        var doc = new EntityDocument
        {
            Title = title,
            FileName = file.FileName,
            FilePath = savedPath,
            FileType = fileTypeKey,
            FileSize = file.Length,
            FileHash = fileHash,
            SubjectId = subjectId,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _docRepo.AddAsync(doc);
        await _docRepo.SaveChangesAsync();
        var subject = await _subjectRepo.GetByIdAsync(doc.SubjectId);

        var resultDto = new DocumentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            FileName = doc.FileName,
            FileType = doc.FileType,
            FileSize = doc.FileSize,
            SubjectId = doc.SubjectId,
            Status = doc.Status,
            CreatedAt = doc.CreatedAt,
            SubjectName = subject?.Name
        };
        await _realtime.SendDocumentCreatedAsync(resultDto);

        return resultDto;
    }

    public async Task DeleteAsync(int id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc is null || doc.IsDeleted) return;

        doc.IsDeleted = true;
        doc.UpdatedAt = DateTime.UtcNow;

        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();

        await _realtime.SendDocumentDeletedAsync(doc.Id, doc.SubjectId);
    }

    public async Task TriggerEmbeddingAsync(int documentId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc is null || doc.IsDeleted) return;

        var statusBeforeProcessing = doc.Status;
        EmbeddingSet? processingSet = null;

        doc.Status = DocumentStatus.Processing;
        doc.UpdatedAt = DateTime.UtcNow;
        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();
        await _realtime.SendDocumentStatusChangedAsync(doc.Id, doc.SubjectId, doc.Status.ToString());
        try
        {
            var activeConfig = await _chunkingConfigService.ResolveActiveConfigAsync(doc.SubjectId);

            await SaveLogAsync(documentId, $"ChunkingConfig: size={activeConfig.ChunkSize}, overlap={activeConfig.ChunkOverlap}, strategy={activeConfig.Strategy}, scope={activeConfig.Scope}, subjectId={activeConfig.SubjectId}", "Info");

            // ====== SỬA LỖI: Kiểm tra tất cả EmbeddingSet đã tồn tại (không chỉ Ready) ======
            var existingSets = await _embeddingSetRepo.GetAllAsync(
                e => e.FileHash == doc.FileHash
                     && e.ChunkingConfigId == activeConfig.Id
            );
            var existingSet = existingSets.FirstOrDefault();

            if (existingSet != null)
            {
                processingSet = existingSet;

                // Nếu đã Ready → tái sử dụng ngay
                if (existingSet.Status == EmbeddingSetStatus.Ready)
                {
                    doc.EmbeddingSetId = existingSet.Id;
                    doc.Status = DocumentStatus.Ready;
                    doc.UpdatedAt = DateTime.UtcNow;
                    _docRepo.Update(doc);
                    await _docRepo.SaveChangesAsync();
                    await SaveLogAsync(documentId, "Tái sử dụng EmbeddingSet có sẵn (trùng nội dung + config)", "Info");
                    await SaveLogAsync(documentId, "Document status set to Ready", "Info");

                    await _realtime.SendDocumentStatusChangedAsync(doc.Id, doc.SubjectId, doc.Status.ToString());
                    await _realtime.SendDocumentProcessingFinishedAsync(documentId, "Reused");
                    return;
                }

                // ====== SỬA LỖI: Processing bị kẹt quá 10 phút → coi như Failed, xử lý lại ======
                if (existingSet.Status == EmbeddingSetStatus.Processing)
                {
                    var lastUpdate = existingSet.UpdatedAt ?? existingSet.CreatedAt;
                    var processingTimeout = TimeSpan.FromMinutes(10);
                    var sourceDocument = existingSet.SourceDocumentId == documentId
                        ? doc
                        : await _docRepo.GetByIdAsync(existingSet.SourceDocumentId);
                    var sourceDocumentUnavailableOrFailed = sourceDocument == null
                        || (existingSet.SourceDocumentId == documentId
                            ? statusBeforeProcessing == DocumentStatus.Failed
                            : sourceDocument.Status == DocumentStatus.Failed);

                    if (!sourceDocumentUnavailableOrFailed && DateTime.UtcNow - lastUpdate < processingTimeout)
                    {
                        // Thật sự đang xử lý (chưa quá 10 phút) → gán document và chờ
                        doc.EmbeddingSetId = existingSet.Id;
                        doc.UpdatedAt = DateTime.UtcNow;
                        _docRepo.Update(doc);
                        await _docRepo.SaveChangesAsync();
                        await SaveLogAsync(documentId, "EmbeddingSet đang được xử lý bởi tiến trình khác, đã gán document để chờ kết quả.", "Info");

                        await _realtime.SendDocumentStatusChangedAsync(doc.Id, doc.SubjectId, doc.Status.ToString());
                        return;
                    }

                    var resetReason = sourceDocumentUnavailableOrFailed
                        ? "tài liệu nguồn đã bị xóa hoặc tiến trình trước đã thất bại"
                        : $"đã Processing quá {processingTimeout.TotalMinutes} phút";
                    await SaveLogAsync(documentId, $"EmbeddingSet bị kẹt vì {resetReason}, reset để xử lý lại.", "Warning");
                }

                // ====== Failed HOẶC Processing bị kẹt → xóa chunks cũ và reset ======
                var oldChunks = await _chunkRepo.GetAllAsync(c => c.EmbeddingSetId == existingSet.Id);
                foreach (var chunk in oldChunks)
                {
                    _chunkRepo.Delete(chunk);
                }
                if (oldChunks.Any())
                {
                    await _chunkRepo.SaveChangesAsync();
                }

                // Reset EmbeddingSet để tái sử dụng
                existingSet.Status = EmbeddingSetStatus.Processing;
                existingSet.SourceDocumentId = doc.Id;
                existingSet.CanonicalTitle = doc.Title;
                existingSet.UpdatedAt = DateTime.UtcNow;
                _embeddingSetRepo.Update(existingSet);
                await _embeddingSetRepo.SaveChangesAsync();

                await SaveLogAsync(documentId, "Reset EmbeddingSet cũ để xử lý lại.", "Info");

                // Gán document vào embedding set
                doc.EmbeddingSetId = existingSet.Id;
                doc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(doc);
                await _docRepo.SaveChangesAsync();

                // Tiếp tục xử lý chunking + embedding
                await ProcessChunksAndEmbeddingsAsync(doc, existingSet, activeConfig, documentId);
                return;
            }

            // ====== Tạo mới EmbeddingSet ======
            var newEmbeddingSet = new EmbeddingSet
            {
                FileHash = doc.FileHash,
                ChunkingConfigId = activeConfig.Id,
                EmbeddingModel = "multilingual-e5-base",
                Status = EmbeddingSetStatus.Processing,
                SourceDocumentId = doc.Id,
                CanonicalTitle = doc.Title,
                CreatedAt = DateTime.UtcNow
            };

            await _embeddingSetRepo.AddAsync(newEmbeddingSet);
            try
            {
                await _embeddingSetRepo.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // ====== SỬA LỖI CHÍNH: Detach entity bị lỗi khỏi DbContext ======
                _context.Entry(newEmbeddingSet).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                // Tìm lại bản ghi đã tồn tại
                var found = (await _embeddingSetRepo.GetAllAsync(
                    e => e.FileHash == doc.FileHash && e.ChunkingConfigId == activeConfig.Id
                )).FirstOrDefault();

                if (found != null)
                {
                    newEmbeddingSet = found;
                    await SaveLogAsync(documentId, "Sử dụng EmbeddingSet đã được tạo bởi tiến trình khác.", "Info");
                }
                else
                {
                    throw; // Không tìm thấy → lỗi thật
                }
            }

            processingSet = newEmbeddingSet;

            // Gán document vào embedding set
            doc.EmbeddingSetId = newEmbeddingSet.Id;
            doc.UpdatedAt = DateTime.UtcNow;
            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();

            // Xử lý chunking + embedding
            await ProcessChunksAndEmbeddingsAsync(doc, newEmbeddingSet, activeConfig, documentId);
        }
        catch (Exception ex)
        {
            if (processingSet != null)
            {
                try
                {
                    processingSet.Status = EmbeddingSetStatus.Failed;
                    processingSet.UpdatedAt = DateTime.UtcNow;
                    _embeddingSetRepo.Update(processingSet);
                    await _embeddingSetRepo.SaveChangesAsync();
                }
                catch (Exception setStatusException)
                {
                    Console.WriteLine($"Failed to mark EmbeddingSet {processingSet.Id} as Failed: {setStatusException}");
                }
            }

            doc.Status = DocumentStatus.Failed;
            doc.UpdatedAt = DateTime.UtcNow;
            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();
            var errorMessage = ex.GetBaseException().Message;
            await SaveLogAsync(documentId, $"Document processing failed: {errorMessage}", "Error");

            await _realtime.SendDocumentStatusChangedAsync(doc.Id, doc.SubjectId, doc.Status.ToString());
            await _realtime.SendDocumentProcessingFinishedAsync(documentId, "Failed");
            throw;
        }
    }

    /// <summary>
    /// Xử lý chunking và embedding cho document (tách ra để tránh code trùng lặp)
    /// </summary>
    private async Task ProcessChunksAndEmbeddingsAsync(
        Document doc, EmbeddingSet embeddingSet, ChunkingConfig activeConfig, int documentId)
    {
        var ext = Path.GetExtension(doc.FilePath).ToLowerInvariant();
        var fileType = (doc.FileType ?? ext.TrimStart('.')).ToLowerInvariant();

        List<(string Content, string SourceLocation)> chunks;

        if (fileType == "pdf" || ext == ".pdf")
        {
            var segments = ExtractPdfSegments(doc.FilePath);
            if (segments.Count == 0)
                throw new InvalidOperationException("Không thể trích xuất văn bản từ file PDF.");
            chunks = ChunkSegmentsByCharacter(segments, activeConfig.ChunkSize, activeConfig.ChunkOverlap);
        }
        else if (fileType == "docx" || ext == ".docx")
        {
            var segments = ExtractDocxSegments(doc.FilePath);
            if (segments.Count == 0)
                throw new InvalidOperationException("Không thể trích xuất văn bản từ file DOCX.");
            chunks = ChunkSegmentsByCharacter(segments, activeConfig.ChunkSize, activeConfig.ChunkOverlap);
        }
        else if (fileType == "pptx" || ext == ".pptx")
        {
            var segments = ExtractPptxSegments(doc.FilePath);
            if (segments.Count == 0)
                throw new InvalidOperationException("Không thể trích xuất văn bản từ file PPTX.");
            chunks = ChunkSegmentsBySlide(segments);
        }
        else
        {
            throw new InvalidOperationException("Chỉ hỗ trợ trích xuất văn bản cho PDF, DOCX và PPTX.");
        }

        if (chunks.Count == 0)
            throw new InvalidOperationException("Không tạo được chunk nào từ nội dung tài liệu.");

        var hfToken = _configuration["HuggingFace:Token"];
        if (string.IsNullOrWhiteSpace(hfToken))
            throw new InvalidOperationException("Hugging Face token không được cấu hình.");

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

        int idx = 0;
        var batchSize = 5;
        var pendingCount = 0;
        foreach (var (text, sourceLocation) in chunks)
        {
            await SaveLogAsync(documentId, $"Processing chunk {idx}", "Processing");

            string formattedText = $"passage: {text}";
            var payload = new { inputs = formattedText };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(modelUrl, content);
            if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                throw new InvalidOperationException("Server AI đang khởi động, vui lòng đợi 20 giây và bấm nút lại!");

            var respJson = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var apiError = string.IsNullOrWhiteSpace(respJson)
                    ? resp.ReasonPhrase
                    : respJson.Length > 500 ? respJson[..500] : respJson;
                throw new InvalidOperationException($"Hugging Face trả về HTTP {(int)resp.StatusCode}: {apiError}");
            }

            using var docJson = JsonDocument.Parse(respJson);
            var vector = new List<float>();
            var root = docJson.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var firstElement = root[0];
                var vectorArray = firstElement.ValueKind == JsonValueKind.Number ? root : firstElement;
                foreach (var el in vectorArray.EnumerateArray()) vector.Add(el.GetSingle());
            }

            var chunkEntity = new DocumentChunk
            {
                EmbeddingSetId = embeddingSet.Id,
                Content = text,
                ChunkIndex = idx++,
                SourceLocation = sourceLocation,
                EmbeddingJson = JsonSerializer.Serialize(vector),
                CreatedAt = DateTime.UtcNow
            };

            await _chunkRepo.AddAsync(chunkEntity);
            pendingCount++;
            if (pendingCount >= batchSize)
            {
                await _chunkRepo.SaveChangesAsync();
                pendingCount = 0;
            }

            var display = text.Length > 2000 ? text.Substring(0, 2000) + "...(truncated)" : text;
            await SaveLogAsync(documentId, $"ChunkContent:{chunkEntity.ChunkIndex}:{display}", "ChunkContent");
            await SaveLogAsync(documentId, $"Chunk {chunkEntity.ChunkIndex} embedded successfully", "Info");
            await Task.Delay(300);
        }
        if (pendingCount > 0) await _chunkRepo.SaveChangesAsync();

        await SaveLogAsync(documentId, "All chunks saved", "Info");

        embeddingSet.Status = EmbeddingSetStatus.Ready;
        embeddingSet.UpdatedAt = DateTime.UtcNow;
        _embeddingSetRepo.Update(embeddingSet);
        await _embeddingSetRepo.SaveChangesAsync();

        doc.EmbeddingSetId = embeddingSet.Id;
        doc.Status = DocumentStatus.Ready;
        doc.UpdatedAt = DateTime.UtcNow;
        _docRepo.Update(doc);
        await SaveLogAsync(documentId, "Document status set to Ready", "Info");
        await _docRepo.SaveChangesAsync();

        await _realtime.SendDocumentStatusChangedAsync(doc.Id, doc.SubjectId, doc.Status.ToString());
        await _realtime.SendDocumentProcessingFinishedAsync(documentId, "Ready");
    }

    /// <summary>
    /// Kiểm tra lỗi có phải vi phạm ràng buộc unique không
    /// </summary>
    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var sqlEx = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
        if (sqlEx == null) return false;
        return sqlEx.Number == 2601 || sqlEx.Number == 2627;
    }

    private async Task SaveLogAsync(int documentId, string message, string status)
    {
        try
        {
            if (_scopeFactory == null)
            {
                var log = new DocumentLog
                {
                    DocumentId = documentId,
                    LogMessage = message,
                    Timestamp = DateTime.UtcNow,
                    Status = status
                };
                await _uow.DocumentLogs.AddAsync(log);
                await _uow.SaveChangesAsync();
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SmartEdu.Data.AppDbContext>();
            var log2 = new DocumentLog
            {
                DocumentId = documentId,
                LogMessage = message,
                Timestamp = DateTime.UtcNow,
                Status = status
            };
            ctx.DocumentLogs.Add(log2);
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save document log: {ex}");
        }

        try
        {
            await _realtime.SendDocumentLogAsync(documentId, message, status);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to broadcast document log via SignalR: {ex}");
        }
    }

    public async Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isStaff, int? subjectId = null)
    {
        IEnumerable<EntityDocument> docs;

        if (isStaff)
        {
            docs = await _docRepo.GetAllWithIncludeAsync(
                d => (!subjectId.HasValue || d.SubjectId == subjectId.Value) && !d.IsDeleted,
                d => d.Subject
            );
        }
        else
        {
            var enrollments = await _studentSubjectRepo.GetAllAsync();
            var allowedSubjectIds = enrollments
                .Where(ss => ss.StudentId == userId && !ss.IsDeleted)
                .Select(ss => ss.SubjectId)
                .ToList();

            docs = await _docRepo.GetAllWithIncludeAsync(
                d => allowedSubjectIds.Contains(d.SubjectId) &&
                     (!subjectId.HasValue || d.SubjectId == subjectId.Value) &&
                     !d.IsDeleted,
                d => d.Subject
            );
        }

        return docs.Select(d => new DocumentDto
        {
            Id = d.Id,
            Title = d.Title,
            FileName = d.FileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            SubjectId = d.SubjectId,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            SubjectName = d.Subject?.Name
        });
    }

    public async Task UpdateTitleAsync(int id, string newTitle)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc is null) throw new InvalidOperationException("Không tìm thấy tài liệu.");

        doc.Title = newTitle;
        doc.UpdatedAt = DateTime.UtcNow;

        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();
        try
        {
            // Notify clients that document metadata (title) changed
            var dto = new SmartEdu.Shared.DTOs.DocumentDto
            {
                Id = doc.Id,
                Title = doc.Title,
                FileName = doc.FileName,
                FileType = doc.FileType,
                FileSize = doc.FileSize,
                SubjectId = doc.SubjectId,
                Status = doc.Status,
                CreatedAt = doc.CreatedAt,
                SubjectName = doc.Subject?.Name
            };
            await _realtime.SendDocumentUpdatedAsync(dto);
        }
        catch (Exception ex)
        {
            // avoid breaking the update flow when realtime fails
            Console.WriteLine($"Failed to send document updated via SignalR: {ex}");
        }
    }

    public async Task<DocumentDownloadDto?> GetFileForDownloadAsync(int id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null || doc.IsDeleted) return null;

        if (!System.IO.File.Exists(doc.FilePath))
        {
            throw new FileNotFoundException("File vật lý không tồn tại trên hệ thống.");
        }

        string fileType = (doc.FileType ?? Path.GetExtension(doc.FilePath)).ToLowerInvariant().TrimStart('.');

        string contentType = fileType switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };

        return new DocumentDownloadDto
        {
            FilePath = doc.FilePath,
            ContentType = contentType,
            FileName = doc.FileName
        };
    }

    public async Task<bool> HasReadyDocumentsAsync(int subjectId)
    {
        var docs = await _docRepo.GetAllAsync(d =>
            d.SubjectId == subjectId &&
            d.Status == SmartEdu.Shared.Enums.DocumentStatus.Ready &&
            !d.IsDeleted);

        return docs.Any();
    }

    public async Task<DuplicateCheckDto> CheckDuplicateAsync(string filePath, string fileExt, string fileHash, int subjectId, int excludeDocumentId = 0)
    {
        // === Bước 1: check trùng 100% (giữ nguyên logic cũ) ===
        var exactMatches = await _docRepo.GetAllAsync(d =>
            d.FileHash == fileHash &&
            d.SubjectId == subjectId &&
            d.Id != excludeDocumentId &&
            !d.IsDeleted);

        var exactDoc = exactMatches.FirstOrDefault();
        if (exactDoc != null)
        {
            return new DuplicateCheckDto
            {
                HasDuplicate = true,
                MatchType = DuplicateMatchType.Exact,
                DuplicateDocumentId = exactDoc.Id,
                DuplicateTitle = exactDoc.Title,
                DuplicateCreatedAt = exactDoc.CreatedAt,
                IsEmbeddingReady = exactDoc.Status == DocumentStatus.Ready || exactDoc.EmbeddingSetId != null
            };
        }

        // === Bước 2: check gần giống ===
        try
        {
            var newDocVector = await ComputeDocumentVectorFromFileAsync(filePath, fileExt, subjectId);
            if (newDocVector.Length == 0)
                return new DuplicateCheckDto { HasDuplicate = false };

            var candidateDocs = await _docRepo.GetAllWithIncludeAsync(
                d => d.SubjectId == subjectId
                     && d.Id != excludeDocumentId
                     && !d.IsDeleted
                     && d.Status == DocumentStatus.Ready
                     && d.EmbeddingSetId != null,
                d => d.EmbeddingSet
            );

            DocumentDto? bestMatch = null;
            double bestScore = 0;

            foreach (var candidate in candidateDocs)
            {
                var candidateChunks = await _chunkRepo.GetAllAsync(c => c.EmbeddingSetId == candidate.EmbeddingSetId!.Value);
                var candidateVectors = candidateChunks
                    .Select(c => JsonSerializer.Deserialize<float[]>(c.EmbeddingJson!))
                    .Where(v => v != null)
                    .ToList();

                if (candidateVectors.Count == 0) continue;

                var candidateDocVector = VectorMath.MeanPool(candidateVectors!);
                var score = VectorMath.CosineSimilarity(newDocVector, candidateDocVector);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = new DocumentDto { Id = candidate.Id, Title = candidate.Title, CreatedAt = candidate.CreatedAt, Status = candidate.Status };
                }
            }
            var nearDuplicateThreshold = await _uploadConfigService.ResolveNearDuplicateThresholdAsync();
            if (bestMatch != null && bestScore >= nearDuplicateThreshold)
            {
                return new DuplicateCheckDto
                {
                    HasDuplicate = true,
                    MatchType = DuplicateMatchType.Near,
                    DuplicateDocumentId = bestMatch.Id,
                    DuplicateTitle = bestMatch.Title,
                    DuplicateCreatedAt = bestMatch.CreatedAt,
                    IsEmbeddingReady = true,
                    SimilarityPercent = Math.Round(bestScore * 100, 1)
                };
            }
        }
        catch (Exception ex)
        {
            // Nếu bước check similarity lỗi (ví dụ HuggingFace tạm downtime), KHÔNG chặn upload —
            // chỉ log lại và coi như không phát hiện trùng, để không làm gián đoạn trải nghiệm giảng viên
            Console.WriteLine($"Lỗi khi kiểm tra tài liệu gần giống: {ex}");
        }

        return new DuplicateCheckDto { HasDuplicate = false };
    }

    private async Task<float[]> ComputeDocumentVectorFromFileAsync(string filePath, string fileExt, int subjectId)
    {
        var activeConfig = await _chunkingConfigService.ResolveActiveConfigAsync(subjectId);
        var ext = fileExt.ToLowerInvariant().TrimStart('.');

        List<(string Content, string SourceLocation)> chunks;

        if (ext == "pdf")
        {
            var segments = ExtractPdfSegments(filePath);
            chunks = ChunkSegmentsByCharacter(segments, activeConfig.ChunkSize, activeConfig.ChunkOverlap);
        }
        else if (ext == "docx")
        {
            var segments = ExtractDocxSegments(filePath);
            chunks = ChunkSegmentsByCharacter(segments, activeConfig.ChunkSize, activeConfig.ChunkOverlap);
        }
        else if (ext == "pptx")
        {
            var segments = ExtractPptxSegments(filePath);
            chunks = ChunkSegmentsBySlide(segments);
        }
        else
        {
            return Array.Empty<float>();
        }

        if (chunks.Count == 0) return Array.Empty<float>();

        // Giới hạn số chunk embed thử để tránh file quá dài làm chậm bước check trùng —
        // lấy mẫu đều nhau xuyên suốt tài liệu thay vì chỉ N chunk đầu, để đại diện tốt hơn
        var sampledChunks = chunks.Count <= MaxChunksForDuplicateCheck
            ? chunks
            : SampleEvenly(chunks, MaxChunksForDuplicateCheck);

        var embeddingTasks = sampledChunks.Select(c => GetChunkEmbeddingForCheckAsync(c.Content)).ToList();
        var results = await Task.WhenAll(embeddingTasks);
        var vectors = results.Where(v => v.Length > 0).ToList();

        return VectorMath.MeanPool(vectors);
    }

    private static List<(string Content, string SourceLocation)> SampleEvenly(
        List<(string Content, string SourceLocation)> chunks, int sampleCount)
    {
        var step = (double)chunks.Count / sampleCount;
        var result = new List<(string, string)>();
        for (int i = 0; i < sampleCount; i++)
        {
            int idx = (int)(i * step);
            if (idx < chunks.Count) result.Add(chunks[idx]);
        }
        return result;
    }

    private async Task<float[]> GetChunkEmbeddingForCheckAsync(string text)
    {
        var hfToken = _configuration["HuggingFace:Token"];
        if (string.IsNullOrWhiteSpace(hfToken)) return Array.Empty<float>();

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);   // giới hạn 15s/request, tránh treo cả upload
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

        try
        {
            var payload = new { inputs = $"passage: {text}" };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(modelUrl, content);
            if (!resp.IsSuccessStatusCode) return Array.Empty<float>();

            var respJson = await resp.Content.ReadAsStringAsync();
            using var docJson = JsonDocument.Parse(respJson);
            var vector = new List<float>();
            var root = docJson.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var firstElement = root[0];
                var vectorArray = firstElement.ValueKind == JsonValueKind.Number ? root : firstElement;
                foreach (var el in vectorArray.EnumerateArray()) vector.Add(el.GetSingle());
            }
            return vector.ToArray();
        }
        catch (TaskCanceledException)
        {
            // Timeout riêng cho request này - bỏ qua, không chặn cả quá trình check
            return Array.Empty<float>();
        }
    }

    public async Task HandleDuplicateAsync(DuplicateHandleDto dto, int currentUserId)
    {
        var newDoc = await _docRepo.GetByIdAsync(dto.NewDocumentId);
        var oldDoc = await _docRepo.GetByIdAsync(dto.OldDocumentId);

        if (newDoc == null || oldDoc == null)
            throw new InvalidOperationException("Tài liệu không tồn tại.");

        var isLeader = await _lecturerSubjectRepo.GetAllAsync(
            ls => ls.LecturerId == currentUserId &&
                  ls.SubjectId == newDoc.SubjectId &&
                  ls.IsLeader);

        if (!isLeader.Any())
            throw new UnauthorizedAccessException("Chỉ trưởng môn học mới có quyền xử lý tài liệu trùng.");

        switch (dto.Action)
        {
            case DocumentDuplicateAction.Ignored:
                newDoc.IsDeleted = true;
                newDoc.UpdatedAt = DateTime.UtcNow;
                newDoc.DuplicateAction = DocumentDuplicateAction.Ignored;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                break;

            case DocumentDuplicateAction.Replaced:
                oldDoc.IsDeleted = true;
                oldDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(oldDoc);

                newDoc.DuplicateAction = DocumentDuplicateAction.Replaced;
                newDoc.ParentDocumentId = oldDoc.Id;
                newDoc.Version = (oldDoc.Version > 0 ? oldDoc.Version : 1) + 1;
                newDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                var subject = await _subjectRepo.GetByIdAsync(newDoc.SubjectId);
                await _realtime.SendDocumentDeletedAsync(oldDoc.Id, oldDoc.SubjectId);
                await _realtime.SendDocumentCreatedAsync(new DocumentDto
                {
                    Id = newDoc.Id,
                    Title = newDoc.Title,
                    FileName = newDoc.FileName,
                    FileType = newDoc.FileType,
                    FileSize = newDoc.FileSize,
                    SubjectId = newDoc.SubjectId,
                    Status = newDoc.Status,
                    CreatedAt = newDoc.CreatedAt,
                    SubjectName = subject?.Name
                });
                break;

            case DocumentDuplicateAction.KeptBoth:
                newDoc.DuplicateAction = DocumentDuplicateAction.KeptBoth;
                newDoc.Version = (oldDoc.Version > 0 ? oldDoc.Version : 1) + 1;
                newDoc.ParentDocumentId = oldDoc.Id;
                newDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                var subjects = await _subjectRepo.GetByIdAsync(newDoc.SubjectId);
                await _realtime.SendDocumentCreatedAsync(new DocumentDto
                {
                    Id = newDoc.Id,
                    Title = newDoc.Title,
                    FileName = newDoc.FileName,
                    FileType = newDoc.FileType,
                    FileSize = newDoc.FileSize,
                    SubjectId = newDoc.SubjectId,
                    Status = newDoc.Status,
                    CreatedAt = newDoc.CreatedAt,
                    SubjectName = subjects?.Name
                });
                break;

            default:
                throw new InvalidOperationException("Hành động không hợp lệ.");
        }
    }

    public async Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isAdmin, bool isLecturer, int? subjectId = null)
    {
        IEnumerable<EntityDocument> docs;

        if (isAdmin)
        {
            docs = await _docRepo.GetAllWithIncludeAsync(
                d => (!subjectId.HasValue || d.SubjectId == subjectId.Value) && !d.IsDeleted,
                d => d.Subject
            );
        }
        else if (isLecturer)
        {
            var lecturerRels = await _lecturerSubjectRepo.GetAllAsync(ls => ls.LecturerId == userId);
            var allowedSubjectIds = lecturerRels.Select(ls => ls.SubjectId).ToList();

            docs = await _docRepo.GetAllWithIncludeAsync(
                d => allowedSubjectIds.Contains(d.SubjectId) &&
                     (!subjectId.HasValue || d.SubjectId == subjectId.Value) &&
                     !d.IsDeleted,
                d => d.Subject
            );
        }
        else
        {
            var enrollments = await _studentSubjectRepo.GetAllAsync();
            var allowedSubjectIds = enrollments
                .Where(ss => ss.StudentId == userId && !ss.IsDeleted)
                .Select(ss => ss.SubjectId)
                .ToList();

            docs = await _docRepo.GetAllWithIncludeAsync(
                d => allowedSubjectIds.Contains(d.SubjectId) &&
                     (!subjectId.HasValue || d.SubjectId == subjectId.Value) &&
                     !d.IsDeleted,
                d => d.Subject
            );
        }

        return docs.Select(d => new DocumentDto
        {
            Id = d.Id,
            Title = d.Title,
            FileName = d.FileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            SubjectId = d.SubjectId,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            SubjectName = d.Subject?.Name
        });
    }

    private static List<TextSegment> ExtractPdfSegments(string path)
    {
        var segments = new List<TextSegment>();
        using var pdf = PdfDocument.Open(path);
        int pageNum = 0;

        foreach (var page in pdf.GetPages())
        {
            pageNum++;
            if (!string.IsNullOrWhiteSpace(page.Text))
            {
                segments.Add(new TextSegment { Text = page.Text, Location = $"Trang {pageNum}" });
            }
        }

        return segments;
    }

    private static List<TextSegment> ExtractDocxSegments(string path)
    {
        var segments = new List<TextSegment>();
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return segments;

        var headingStyleIds = new HashSet<string>();
        var stylesPart = doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylesPart?.Styles != null)
        {
            foreach (var style in stylesPart.Styles.Elements<DocumentFormat.OpenXml.Wordprocessing.Style>())
            {
                var styleName = style.StyleName?.Val?.Value ?? string.Empty;
                if ((styleName.Contains("Heading", StringComparison.OrdinalIgnoreCase)
                     || styleName.Contains("Title", StringComparison.OrdinalIgnoreCase))
                    && style.StyleId?.Value != null)
                {
                    headingStyleIds.Add(style.StyleId.Value);
                }
            }
        }

        var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
        bool documentHasAnyHeading = paragraphs.Any(p =>
        {
            var styleId = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            return styleId != null && headingStyleIds.Contains(styleId) && !string.IsNullOrWhiteSpace(p.InnerText);
        });

        var buffer = new StringBuilder();
        string? currentHeading = null;
        int bufferStartPara = 1;
        const int fallbackFlushEveryNParas = 5; // khi không có heading, flush theo lô nhỏ

        void Flush(int endParaIndex)
        {
            if (buffer.Length == 0) return;
            var location = currentHeading != null
                ? $"Mục: {currentHeading}"
                : $"Đoạn {bufferStartPara}-{endParaIndex}";
            segments.Add(new TextSegment { Text = buffer.ToString(), Location = location });
            buffer.Clear();
        }

        for (int i = 0; i < paragraphs.Count; i++)
        {
            int paraIndex = i + 1;
            var para = paragraphs[i];
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            bool isHeading = styleId != null && headingStyleIds.Contains(styleId);
            var text = para.InnerText;

            if (isHeading && !string.IsNullOrWhiteSpace(text))
            {
                Flush(paraIndex - 1);
                currentHeading = text.Trim();
                bufferStartPara = paraIndex + 1;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                buffer.AppendLine(text);
            }

            // Nếu tài liệu KHÔNG có heading nào, flush theo lô nhỏ để giữ độ chi tiết vị trí
            if (!documentHasAnyHeading && (paraIndex - bufferStartPara + 1) >= fallbackFlushEveryNParas)
            {
                Flush(paraIndex);
                bufferStartPara = paraIndex + 1;
            }
        }
        Flush(paragraphs.Count);

        return segments;
    }

    private static List<TextSegment> ExtractPptxSegments(string path)
    {
        var raw = new List<TextSegment>();
        using var ppt = PresentationDocument.Open(path, false);
        var presentationPart = ppt.PresentationPart;
        var slideIdList = presentationPart?.Presentation?.SlideIdList;
        if (slideIdList == null) return raw;

        int slideNumber = 0;
        foreach (var slideId in slideIdList.Elements<DocumentFormat.OpenXml.Presentation.SlideId>())
        {
            slideNumber++;
            var relId = slideId.RelationshipId?.Value;
            if (relId == null) continue;

            var slidePart = (SlidePart)presentationPart!.GetPartById(relId);
            var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t));

            var slideText = string.Join(" ", texts).Trim();
            if (!string.IsNullOrWhiteSpace(slideText))
            {
                raw.Add(new TextSegment { Text = slideText, Location = $"Slide {slideNumber}" });
            }
        }

        // Gộp các slide quá ngắn (< 50 ký tự) với slide kế tiếp
        const int minLength = 50;
        var merged = new List<TextSegment>();
        TextSegment? pending = null;

        foreach (var seg in raw)
        {
            if (pending == null)
            {
                pending = new TextSegment { Text = seg.Text, Location = seg.Location };
            }
            else
            {
                pending.Text += "\n" + seg.Text;
                pending.Location += $", {seg.Location}";
            }

            if (pending.Text.Length >= minLength)
            {
                merged.Add(pending);
                pending = null;
            }
        }
        if (pending != null) merged.Add(pending);

        return merged;
    }

    private static List<(string Content, string SourceLocation)> ChunkSegmentsByCharacter(
    List<TextSegment> segments, int chunkSize, int chunkOverlap)
    {
        var result = new List<(string Content, string SourceLocation)>();
        if (segments.Count == 0) return result;

        var fullText = new StringBuilder();
        var boundaries = new List<(int Start, int End, string Location)>();

        foreach (var seg in segments)
        {
            int start = fullText.Length;
            fullText.Append(seg.Text).Append('\n');
            boundaries.Add((start, fullText.Length, seg.Location));
        }

        string text = fullText.ToString();
        int step = Math.Max(1, chunkSize - chunkOverlap);
        int pos = 0;

        while (pos < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - pos);
            string chunkText = text.Substring(pos, len).Trim();
            int chunkEnd = pos + len;

            var overlapping = boundaries
                .Where(b => b.Start < chunkEnd && b.End > pos)
                .Select(b => b.Location)
                .Distinct()
                .ToList();

            string location = overlapping.Count switch
            {
                0 => "Không xác định",
                1 => overlapping[0],
                _ => $"{overlapping.First()} → {overlapping.Last()}"
            };

            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                result.Add((chunkText, location));
            }

            pos += step;
        }

        return result;
    }

    private static List<(string Content, string SourceLocation)> ChunkSegmentsBySlide(List<TextSegment> segments)
    {
        return segments.Select(s => (s.Text, s.Location)).ToList();
    }
}
