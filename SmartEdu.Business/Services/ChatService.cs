using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartEdu.Business.Services;

public class ChatService : IChatService
{
    private readonly IRepository<ChatSession> _sessionRepo;
    private readonly IRepository<ChatMessage> _messageRepo;
    private readonly IRepository<DocumentChunk> _chunkRepo;
    private readonly IRepository<UserSubscription> _subscriptionRepo;
    private readonly IRepository<UsageLog> _usageLogRepo;
    private readonly ILogger<ChatService> _logger;

    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly IRealtimeNotifier _realtime;

    public ChatService(
    IRepository<ChatSession> sessionRepo,
    IRepository<ChatMessage> messageRepo,
    IRepository<DocumentChunk> chunkRepo,
    IRepository<UserSubscription> subscriptionRepo,
    IRepository<UsageLog> usageLogRepo,
    IHttpClientFactory httpFactory,
    IConfiguration configuration,
    ILogger<ChatService> logger,
    IRealtimeNotifier realtime)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _chunkRepo = chunkRepo;
        _subscriptionRepo = subscriptionRepo;
        _usageLogRepo = usageLogRepo;
        _httpFactory = httpFactory;
        _configuration = configuration;
        _logger = logger;
        _realtime = realtime;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request)
    {
        if (!request.SubjectId.HasValue || request.SubjectId.Value <= 0)
        {
            return new ChatResponseDto { SessionId = request.SessionId, Answer = "Vui lòng chọn môn học.", Sources = new List<string>() };
        }

        var sessions = await _sessionRepo.GetAllWithIncludeAsync(
            s => s.SessionId == request.SessionId,
            s => s.Subject
        );
        var session = sessions.FirstOrDefault();

        if (session == null)
        {
            session = new ChatSession
            {
                SessionId = request.SessionId,
                SubjectId = request.SubjectId,
                Title = request.Question.Length > 30 ? request.Question[..30] + "..." : request.Question,
                UserId = request.UserId
            };
            await _sessionRepo.AddAsync(session);
            await _sessionRepo.SaveChangesAsync();

            // reload lại kèm Subject sau khi tạo mới, vì SubjectId vừa set có thể chưa có navigation load
            var reloaded = await _sessionRepo.GetAllWithIncludeAsync(
                s => s.Id == session.Id,
                s => s.Subject
            );
            session = reloaded.FirstOrDefault() ?? session;
        }
        else if (session.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên chat này.");
        }

        var ragResponse = await RunRagPipelineAsync(request, session);

        return ragResponse;
    }

    private async Task<ChatResponseDto> RunRagPipelineAsync(ChatRequestDto request, ChatSession session)
    {
        var userMessage = new ChatMessage { ChatSessionId = session.Id, Role = "user", Content = request.Question };
        await _messageRepo.AddAsync(userMessage);
        await _messageRepo.SaveChangesAsync();

        try
        {
            await _realtime.SendChatMessageAsync(request.SessionId, "user", request.Question, null, request.ConnectionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to broadcast user message: {ex}");
        }

        float[] queryVector = await GetHuggingFaceEmbeddingAsync(request.Question);

        var chunks = await _chunkRepo.GetAllWithIncludeAsync(
            c => c.EmbeddingSet != null
                 && c.EmbeddingSet.Status == EmbeddingSetStatus.Ready
                 && c.EmbeddingSet.Documents.Any(d => d.SubjectId == request.SubjectId.Value && !d.IsDeleted),
            c => c.EmbeddingSet,
            c => c.EmbeddingSet.Documents
        );

        // === Retrieval: lấy tối đa N chunk tốt nhất MỖI tài liệu, rồi gộp và cắt tổng ===
        const int maxChunksPerDocument = 3;
        const int maxTotalChunks = 5;

        var allScored = chunks.Select(c => new { Chunk = c, Score = CosineSimilarity(queryVector, JsonSerializer.Deserialize<float[]>(c.EmbeddingJson)) })
                              .OrderByDescending(s => s.Score)
                              .ToList();

        var contentBuilder = new StringBuilder();
        var citations = new List<CitationDto>();
        var chunkIds = new List<int>();
        int citationIndex = 1;

        var groupedByDocument = allScored
            .GroupBy(item => !string.IsNullOrWhiteSpace(item.Chunk.EmbeddingSet?.CanonicalTitle)
                ? item.Chunk.EmbeddingSet.CanonicalTitle
                : "Không xác định")
            .SelectMany(g => g.OrderByDescending(x => x.Score).Take(maxChunksPerDocument))
            .OrderByDescending(x => x.Score)
            .Take(maxTotalChunks)
            .ToList();

        foreach (var item in groupedByDocument)
        {
            var title = !string.IsNullOrWhiteSpace(item.Chunk.EmbeddingSet?.CanonicalTitle)
                ? item.Chunk.EmbeddingSet.CanonicalTitle
                : "Không xác định";

            try
            {
                _logger?.LogInformation("Chunk {ChunkId}: CanonicalTitle={Title}, Score={Score}", item.Chunk.Id, title, item.Score);
            }
            catch { /* swallow logging errors */ }

            contentBuilder.AppendLine($"[{citationIndex}] Nguồn: {title}\n{item.Chunk.Content}\n---\n");

            var relevantDoc = item.Chunk.EmbeddingSet?.Documents
                ?.FirstOrDefault(d => d.SubjectId == request.SubjectId.Value && !d.IsDeleted);

            int resolvedDocumentId = relevantDoc?.Id ?? item.Chunk.EmbeddingSet?.SourceDocumentId ?? 0;

            citations.Add(new CitationDto
            {
                Number = citationIndex,
                DocumentTitle = title,
                ChunkId = item.Chunk.Id,
                DocumentId = resolvedDocumentId
            });
            chunkIds.Add(item.Chunk.Id);
            citationIndex++;
        }

        var (answer, promptTokens, completionTokens) = await GenerateGeminiResponseAsync(contentBuilder.ToString(), request.Question);

        // === Hậu kiểm: chỉ giữ citation có số [n] thực sự xuất hiện trong câu trả lời ===
        var usedCitations = FilterCitationsUsedInAnswer(answer, citations);
        var usedChunkIds = usedCitations.Select(c => c.ChunkId).ToList();

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = answer,
            SourceChunkIds = JsonSerializer.Serialize(usedChunkIds),
            CitationsJson = JsonSerializer.Serialize(usedCitations)
        };
        await _messageRepo.AddAsync(assistantMessage);
        await _messageRepo.SaveChangesAsync();

        try
        {
            await _realtime.SendChatMessageAsync(request.SessionId, "assistant", answer, usedCitations, request.ConnectionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to broadcast chat message: {ex}");
        }

        return new ChatResponseDto
        {
            SessionId = request.SessionId,
            Answer = answer,
            Sources = usedCitations.Select(c => c.DocumentTitle).Distinct().ToList(),
            Citations = usedCitations,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };
    }

    private static List<CitationDto> FilterCitationsUsedInAnswer(string answer, List<CitationDto> allCitations)
    {
        if (string.IsNullOrWhiteSpace(answer) || allCitations.Count == 0)
            return new List<CitationDto>();

        var usedNumbers = new HashSet<int>();
        var matches = System.Text.RegularExpressions.Regex.Matches(answer, @"\[(\d+)\]");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int num))
            {
                usedNumbers.Add(num);
            }
        }

        if (usedNumbers.Count == 0)
            return allCitations;

        return allCitations.Where(c => usedNumbers.Contains(c.Number)).ToList();
    }

    public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId, string userId)
    {
        int uid = int.Parse(userId);

        var sessions = await _sessionRepo.GetAllAsync(s => s.SessionId == sessionId && s.UserId == uid);
        var session = sessions.FirstOrDefault();
        if (session is null) return Enumerable.Empty<ChatMessageDto>();

        var messages = await _messageRepo.GetAllAsync(m => m.ChatSessionId == session.Id);
        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content?.Trim(),
                CreatedAt = m.CreatedAt,
                SourceChunkIds = string.IsNullOrWhiteSpace(m.SourceChunkIds) ? null : JsonSerializer.Deserialize<List<int>>(m.SourceChunkIds),
                Citations = string.IsNullOrWhiteSpace(m.CitationsJson) ? null : JsonSerializer.Deserialize<List<CitationDto>>(m.CitationsJson)
            }).ToList();
    }

    private async Task<float[]> GetHuggingFaceEmbeddingAsync(string question)
    {
        var hfToken = _configuration["HuggingFace:Token"];
        if (string.IsNullOrWhiteSpace(hfToken)) throw new Exception("Thiếu HuggingFace Token.");

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

        var payload = new { inputs = $"query: {question}" };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await client.PostAsync(modelUrl, content);
        resp.EnsureSuccessStatusCode();
        var respJson = await resp.Content.ReadAsStringAsync();

        using var docJson = JsonDocument.Parse(respJson);
        var root = docJson.RootElement;
        var vectorArray = root.ValueKind == JsonValueKind.Array && root[0].ValueKind != JsonValueKind.Number ? root[0] : root;

        return vectorArray.EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    private async Task<(string answer, int promptTokens, int completionTokens)> GenerateGeminiResponseAsync(string context, string question)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "myKey")
            throw new Exception("Thiếu Gemini API Key hợp lệ trong User Secrets.");

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey.Trim()}";

        string fullPrompt = $@"Bạn là trợ lý học tập thông minh (SmartEdu AI).
Nhiệm vụ của bạn là trả lời câu hỏi dựa trên các đoạn ngữ cảnh trích từ tài liệu, mỗi đoạn có đánh số [1], [2], [3]...
Khi trả lời, hãy chèn số trích dẫn tương ứng ngay sau thông tin bạn lấy từ đoạn đó, ví dụ: ""Theo tài liệu, X là Y[1].""
Luôn trả lời bằng tiếng Việt tự nhiên, lịch sự. Nếu thông tin không có, hãy nói 'Tôi không tìm thấy thông tin'.

NGỮ CẢNH:
{context}

CÂU HỎI:
{question}";

        var payload = new
        {
            contents = new[]
        {
            new { parts = new[] { new { text = fullPrompt } } }
        },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 8192
            }
        };

        var client = _httpFactory.CreateClient();
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await client.PostAsync(url, content);
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Lỗi báo từ Google: {respJson}");
        }

        using var docJson = JsonDocument.Parse(respJson);
        var root = docJson.RootElement;
        var candidates = root.GetProperty("candidates");

        if (candidates.GetArrayLength() == 0) return ("Không có phản hồi từ AI.", 0, 0);

        var contentElement = candidates[0].GetProperty("content");
        var parts = contentElement.GetProperty("parts");

        StringBuilder fullAnswer = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
            {
                fullAnswer.Append(textElement.GetString());
            }
        }

        string finalResult = fullAnswer.ToString().Trim();
        if (string.IsNullOrEmpty(finalResult)) finalResult = "Không có phản hồi từ AI.";

        int promptTokens = 0;
        int completionTokens = 0;

        if (root.TryGetProperty("usageMetadata", out var usageMetadata))
        {
            if (usageMetadata.TryGetProperty("promptTokenCount", out var pt))
                promptTokens = pt.GetInt32();
            if (usageMetadata.TryGetProperty("candidatesTokenCount", out var ct))
                completionTokens = ct.GetInt32();
        }

        return (finalResult, promptTokens, completionTokens);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    public async Task<IEnumerable<ChatSessionDto>> GetSessionsByUserIdAsync(string userId)
    {
        int uid = int.Parse(userId);
        var sessions = await _sessionRepo.GetAllWithIncludeAsync(
            s => !s.IsDeleted && s.UserId == uid,
            s => s.Subject,
            s => s.Messages
        );

        return sessions
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new ChatSessionDto
            {
                SessionId = s.SessionId,
                Title = s.Title,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject != null ? s.Subject.Name : "Tất cả",
                MessageCount = s.Messages?.Count ?? 0
            }).ToList();
    }

    public async Task DeleteSessionAsync(string sessionId, string userId)
    {
        int uid = int.Parse(userId);
        var sessions = await _sessionRepo.GetAllAsync(s => s.SessionId == sessionId && s.UserId == uid);
        var session = sessions.FirstOrDefault();

        if (session is null)
        {
            return;
        }

        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;

        _sessionRepo.Update(session);
        await _sessionRepo.SaveChangesAsync();

        try
        {
            await _realtime.SendSessionDeletedAsync(uid, sessionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to broadcast session deleted: {ex}");
        }
    }

    public async Task<ChatResponseDto> ProcessChatWithBillingAsync(ChatRequestDto request)
    {
        var activeSubs = await _subscriptionRepo.GetAllAsync(s =>
    s.UserId == request.UserId && s.Status == SubscriptionStatus.Active && !s.IsDeleted);
        var activeSub = activeSubs.FirstOrDefault();

        if (activeSub == null || activeSub.EndDate < DateTime.UtcNow || activeSub.RemainingTokenQuota <= 0)
            throw new Exception("Gói dịch vụ không hợp lệ hoặc đã hết hạn/hết token.");

        var response = await AskAsync(request);

        if (response.TotalTokens > 0)
        {
            activeSub.RemainingTokenQuota = Math.Max(0, activeSub.RemainingTokenQuota - response.TotalTokens);
            _subscriptionRepo.Update(activeSub);

            await _usageLogRepo.AddAsync(new UsageLog
            {
                UserId = request.UserId,
                Feature = FeatureType.Chat,
                ModelUsed = "gemini-2.5-flash",
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                CreatedAt = DateTime.UtcNow
            });
            await _subscriptionRepo.SaveChangesAsync();
        }

        response.RemainingTokenQuota = activeSub.RemainingTokenQuota;

        try
        {
            await _realtime.SendTokenQuotaUpdatedAsync(request.UserId, activeSub.RemainingTokenQuota);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to broadcast token quota update: {ex}");
        }

        return response;
    }

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
    {
        var subs = await _subscriptionRepo.GetAllAsync(s =>
            s.UserId == userId && s.Status == SubscriptionStatus.Active && !s.IsDeleted);
        return subs.OrderByDescending(s => s.EndDate).FirstOrDefault();
    }
}