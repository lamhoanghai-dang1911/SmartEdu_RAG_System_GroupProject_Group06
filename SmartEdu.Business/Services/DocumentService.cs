using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
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
    IRepository<LecturerSubject> lecturerSubjectRepo)
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
    }

    public async Task<IEnumerable<DocumentChunkDto>> GetChunksByDocumentIdAsync(int documentId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc?.EmbeddingSetId == null) return Enumerable.Empty<DocumentChunkDto>();

        var chunks = await _chunkRepo.GetAllAsync(c => c.EmbeddingSetId == doc.EmbeddingSetId.Value);
        var title = doc.Title;

        return chunks
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new DocumentChunkDto
            {
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                DocumentTitle = title
            });
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
        if (ext is not ".pdf" and not ".docx")
            throw new InvalidOperationException("Chỉ hỗ trợ PDF và DOCX.");

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
            FileType = ext.TrimStart('.'),
            FileSize = file.Length,
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

    public async Task DeleteAsync(int id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc is null || doc.IsDeleted) return;

        doc.IsDeleted = true;
        doc.UpdatedAt = DateTime.UtcNow;

        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();
    }

    public async Task TriggerEmbeddingAsync(int documentId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc is null || doc.IsDeleted) return;

        doc.Status = DocumentStatus.Processing;
        doc.UpdatedAt = DateTime.UtcNow;
        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();

        try
        {
            var activeConfig = await _chunkingConfigService.ResolveActiveConfigAsync(doc.SubjectId);

            var existingSets = await _embeddingSetRepo.GetAllAsync(
                e => e.FileHash == doc.FileHash
                     && e.ChunkingConfigId == activeConfig.Id
                     && e.Status == EmbeddingSetStatus.Ready
            );
            var existingSet = existingSets.FirstOrDefault();

            if (existingSet != null)
            {
                doc.EmbeddingSetId = existingSet.Id;
                doc.Status = DocumentStatus.Ready;
                doc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(doc);
                await _docRepo.SaveChangesAsync();
                await SaveLogAsync(documentId, "Tái sử dụng EmbeddingSet có sẵn (trùng nội dung + config)", "Info");
                return;
            }

            // Không có sẵn -> tạo EmbeddingSet mới rồi chunk/embed như cũ
            var embeddingSet = new EmbeddingSet
            {
                FileHash = doc.FileHash,
                ChunkingConfigId = activeConfig.Id,
                EmbeddingModel = "multilingual-e5-base",
                Status = EmbeddingSetStatus.Processing,
                SourceDocumentId = doc.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _embeddingSetRepo.AddAsync(embeddingSet);
            await _embeddingSetRepo.SaveChangesAsync();

            var ext = Path.GetExtension(doc.FilePath).ToLowerInvariant();
            var fileType = (doc.FileType ?? ext.TrimStart('.')).ToLowerInvariant();
            string rawText = string.Empty;

            if (fileType == "pdf" || ext == ".pdf")
            {
                using var pdf = PdfDocument.Open(doc.FilePath);
                var sb = new StringBuilder();
                foreach (var page in pdf.GetPages()) sb.AppendLine(page.Text);
                rawText = sb.ToString();
            }
            else if (fileType == "docx" || ext == ".docx")
            {
                rawText = ExtractTextFromDocx(doc.FilePath);
            }
            else
            {
                throw new InvalidOperationException("Chỉ hỗ trợ trích xuất văn bản cho PDF và DOCX.");
            }

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("Không thể trích xuất văn bản từ file.");

            var chunks = ChunkText(rawText, activeConfig.ChunkSize, activeConfig.ChunkOverlap / (double)activeConfig.ChunkSize);

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
            foreach (var text in chunks)
            {
                await SaveLogAsync(documentId, $"Processing chunk {idx}", "Processing");

                string formattedText = $"passage: {text}";
                var payload = new { inputs = formattedText };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(modelUrl, content);
                if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    throw new InvalidOperationException("Server AI đang khởi động, vui lòng đợi 20 giây và bấm nút lại!");

                resp.EnsureSuccessStatusCode();
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

                var chunkEntity = new DocumentChunk
                {
                    EmbeddingSetId = embeddingSet.Id,
                    Content = text,
                    ChunkIndex = idx++,
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
        }
        catch (Exception)
        {
            doc.Status = DocumentStatus.Failed;
            doc.UpdatedAt = DateTime.UtcNow;
            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();
            await SaveLogAsync(documentId, "Document processing failed", "Error");
            throw;
        }
    }

    private async Task SaveLogAsync(int documentId, string message, string status)
    {
        try
        {
            if (_scopeFactory == null)
            {
                // fallback: try via unit of work
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
    }
    private static string ExtractTextFromDocx(string path)
    {
        var sb = new StringBuilder();
        using (var doc = WordprocessingDocument.Open(path, false))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;
                foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                }
        }
        return sb.ToString();
    }

    private static IEnumerable<string> ChunkText(string text, int chunkSize = 800, double overlapFraction = 0.1)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        int overlap = (int)Math.Round(chunkSize * overlapFraction);
        int step = Math.Max(1, chunkSize - overlap);
        int pos = 0;
        while (pos < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - pos);
            yield return text.Substring(pos, len).Trim();
            pos += step;
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

    public async Task<DuplicateCheckDto> CheckDuplicateAsync(string fileHash, int subjectId)
    {
        var existing = await _docRepo.GetAllAsync(d =>
            d.FileHash == fileHash &&
            d.SubjectId == subjectId &&
            !d.IsDeleted);

        var doc = existing.FirstOrDefault();
        if (doc == null)
        {
            return new DuplicateCheckDto { HasDuplicate = false };
        }

        return new DuplicateCheckDto
        {
            HasDuplicate = true,
            DuplicateDocumentId = doc.Id,
            DuplicateTitle = doc.Title,
            DuplicateCreatedAt = doc.CreatedAt
        };
    }

    public async Task HandleDuplicateAsync(DuplicateHandleDto dto, int currentUserId)
    {
        var newDoc = await _docRepo.GetByIdAsync(dto.NewDocumentId);
        var oldDoc = await _docRepo.GetByIdAsync(dto.OldDocumentId);

        if (newDoc == null || oldDoc == null)
            throw new InvalidOperationException("Tài liệu không tồn tại.");

        // Check quyền: chỉ leader của subject mới có quyền
        var isLeader = await _lecturerSubjectRepo.GetAllAsync(
            ls => ls.LecturerId == currentUserId &&
                  ls.SubjectId == newDoc.SubjectId &&
                  ls.IsLeader);

        if (!isLeader.Any())
            throw new UnauthorizedAccessException("Chỉ trưởng môn học mới có quyền xử lý tài liệu trùng.");

        switch (dto.Action)
        {
            case DocumentDuplicateAction.Ignored:
                // Xóa tài liệu mới, giữ tài liệu cũ
                newDoc.IsDeleted = true;
                newDoc.UpdatedAt = DateTime.UtcNow;
                newDoc.DuplicateAction = DocumentDuplicateAction.Ignored;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                break;

            case DocumentDuplicateAction.Replaced:
                // Mark tài liệu cũ là deleted, tài liệu mới thay thế
                oldDoc.IsDeleted = true;
                oldDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(oldDoc);

                newDoc.DuplicateAction = DocumentDuplicateAction.Replaced;
                newDoc.ParentDocumentId = oldDoc.Id;
                newDoc.Version = (oldDoc.Version > 0 ? oldDoc.Version : 1) + 1;
                newDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                break;

            case DocumentDuplicateAction.KeptBoth:
                // Giữ cả 2, đánh dấu version
                newDoc.DuplicateAction = DocumentDuplicateAction.KeptBoth;
                newDoc.Version = (oldDoc.Version > 0 ? oldDoc.Version : 1) + 1;
                newDoc.ParentDocumentId = oldDoc.Id;
                newDoc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(newDoc);
                await _docRepo.SaveChangesAsync();
                break;

            default:
                throw new InvalidOperationException("Hành động không hợp lệ.");
        }
    }
}