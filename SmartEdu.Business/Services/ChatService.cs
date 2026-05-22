using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;
using SmartEdu.Shared.Enums;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Business.Services;

public class ChatService : IChatService
{
    private readonly IRepository<ChatSession> _sessionRepo;
    private readonly IRepository<ChatMessage> _messageRepo;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;

    public ChatService(
        IRepository<ChatSession> sessionRepo,
        IRepository<ChatMessage> messageRepo,
        AppDbContext context,
        IHttpClientFactory httpFactory,
        IConfiguration configuration)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _context = context;
        _httpFactory = httpFactory;
        _configuration = configuration;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request)
    {
        // Yêu cầu bắt buộc chọn môn học để giới hạn phạm vi tìm kiếm tài liệu.
        if (!request.SubjectId.HasValue || request.SubjectId.Value <= 0)
        {
            return new ChatResponseDto
            {
                SessionId = request.SessionId,
                Answer = "Vui lòng chọn một môn học cụ thể trước khi hỏi.",
                Sources = new List<string>()
            };
        }

        // 1. TÌM HOẶC TẠO SESSION
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

        if (session is null)
        {
            session = new ChatSession
            {
                SessionId = request.SessionId,
                SubjectId = request.SubjectId,
                Title = request.Question.Length > 50
                    ? request.Question[..50] + "..."
                    : request.Question
            };
            await _sessionRepo.AddAsync(session);
            await _sessionRepo.SaveChangesAsync();
        }

        // 2. LƯU CÂU HỎI USER
        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "user",
            Content = request.Question
        };
        await _messageRepo.AddAsync(userMessage);
        await _messageRepo.SaveChangesAsync();

        // 3. PIPELINE RAG - BƯỚC A: NHÚNG CÂU HỎI (Hugging Face E5)
        float[] queryVector;
        try
        {
            queryVector = await GetHuggingFaceEmbeddingAsync(request.Question);
        }
        catch (Exception ex)
        {
            return await SaveAndReturnErrorAsync(session.Id, request.SessionId, $"Lỗi nhúng câu hỏi: {ex.Message}");
        }

        // 4. PIPELINE RAG - BƯỚC B: TÌM KIẾM VECTOR TƯƠNG ĐỒNG (Retrieval)
        // Lọc theo SubjectId ngay ở truy vấn DB để không quét toàn bộ DocumentChunks khi SubjectId rỗng
        var chunksQuery = _context.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document != null && c.Document.Status == DocumentStatus.Ready && c.Document.SubjectId == request.SubjectId.Value);

        var chunks = await chunksQuery.ToListAsync();

        var scoredChunks = new List<(DocumentChunk chunk, double score)>();
        foreach (var c in chunks)
        {
            if (string.IsNullOrWhiteSpace(c.EmbeddingJson)) continue;
            try
            {
                var docVector = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson);
                if (docVector == null) continue;

                double sim = CosineSimilarity(queryVector, docVector);
                scoredChunks.Add((chunk: c, score: sim));
            }
            catch { continue; }
        }

        // Lấy Top 3 đoạn văn giống nhất
        var topChunks = scoredChunks.OrderByDescending(s => s.score).Take(3).ToList();

        // 5. PIPELINE RAG - BƯỚC C: SINH CÂU TRẢ LỜI VỚI GEMINI
        var contextBuilder = new StringBuilder();
        var sources = new HashSet<string>();

        if (topChunks.Any())
        {
            foreach (var (chunk, score) in topChunks)
            {
                contextBuilder.AppendLine($"[Nguồn: {chunk.Document.Title}]\n{chunk.Content}\n---\n");
                if (!string.IsNullOrWhiteSpace(chunk.Document.Title))
                    sources.Add(chunk.Document.Title);
            }
        }
        else
        {
            contextBuilder.AppendLine("Không có dữ liệu ngữ cảnh nào trong hệ thống khớp với câu hỏi.");
        }

        string answerText;
        try
        {
            answerText = await GenerateGeminiResponseAsync(contextBuilder.ToString(), request.Question);
        }
        catch (Exception ex)
        {
            string friendlyMessage;

            // Phân loại lỗi để hiển thị UI thân thiện
            if (ex.Message.Contains("429") || ex.Message.Contains("RESOURCE_EXHAUSTED"))
            {
                friendlyMessage = "AI hiện đang quá tải lượt sử dụng. Bạn vui lòng quay lại sau ít phút hoặc thử lại vào ngày mai nhé!";
            }
            else if (ex.Message.Contains("401") || ex.Message.Contains("API_KEY_INVALID"))
            {
                friendlyMessage = "Hệ thống đang gặp sự cố về cấu hình, kỹ thuật viên đang xử lý. Xin lỗi vì sự bất tiện này!";
            }
            else
            {
                friendlyMessage = "Có lỗi xảy ra trong quá trình xử lý. Bạn vui lòng thử lại sau nhé!";
            }

            // Lưu thông báo thân thiện này vào DB thay vì lỗi thô
            return await SaveAndReturnErrorAsync(session.Id, request.SessionId, friendlyMessage);
        }

        // 6. LƯU CÂU TRẢ LỜI CỦA AI VÀ TRẢ VỀ
        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = answerText
        };
        await _messageRepo.AddAsync(assistantMessage);
        await _messageRepo.SaveChangesAsync();

        return new ChatResponseDto
        {
            SessionId = request.SessionId,
            Answer = answerText,
            Sources = sources.ToList()
        };
    }

    // --- CÁC HÀM GET LỊCH SỬ CHAT (Giữ nguyên của bạn) ---
    public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId)
    {
        var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null) return Enumerable.Empty<ChatMessageDto>();

        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == session.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToListAsync();
    }

    public async Task<IEnumerable<ChatSessionDto>> GetAllSessionsAsync()
    {
        return await _context.ChatSessions
            .Where(s => !s.IsDeleted)
            .Include(s => s.Subject)
            .Include(s => s.Messages)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ChatSessionDto
            {
                Id = s.Id,
                SessionId = s.SessionId,
                Title = s.Title,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject != null ? s.Subject.Name : "Tất cả",
                MessageCount = s.Messages.Count,
                CreatedAt = s.CreatedAt
            }).ToListAsync();
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null) return;
        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;
        _sessionRepo.Update(session);
        await _sessionRepo.SaveChangesAsync();
    }

    // =========================================================================
    // PRIVATE HELPERS CHO RAG PIPELINE
    // =========================================================================

    private async Task<float[]> GetHuggingFaceEmbeddingAsync(string question)
    {
        var hfToken = _configuration["HuggingFace:Token"];
        if (string.IsNullOrWhiteSpace(hfToken)) throw new Exception("Thiếu HuggingFace Token.");

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

        // Model E5 yêu cầu prefix 'query: ' cho câu hỏi
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

    private async Task<string> GenerateGeminiResponseAsync(string context, string question)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "myKey")
            throw new Exception("Thiếu Gemini API Key hợp lệ trong User Secrets.");

        // Đảm bảo endpoint gọi bản 2.5-flash ổn định
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey.Trim()}";

        string fullPrompt = @"Bạn là trợ lý học tập thông minh (SmartEdu AI).
Nhiệm vụ của bạn là trả lời câu hỏi dựa trên các đoạn ngữ cảnh trích từ tài liệu. Luôn trả lời bằng tiếng Việt tự nhiên, lịch sự. Nếu thông tin không có, hãy nói 'Tôi không tìm thấy thông tin'.

NGỮ CẢNH:
" + context + @"

CÂU HỎI:
" + question;

        // Cấu trúc Payload chuẩn hóa 100% cho Gemini 2.x - Đưa maxOutputTokens vào đúng vị trí
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = fullPrompt } } }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 2048 // Nâng hẳn hạn mức lên 2048 để AI thoải mái viết dài
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

        if (candidates.GetArrayLength() == 0) return "Không có phản hồi từ AI.";

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
        return string.IsNullOrEmpty(finalResult) ? "Không có phản hồi từ AI." : finalResult;
    }

    private async Task<ChatResponseDto> SaveAndReturnErrorAsync(int sessionId, string sessionGuid, string errorMsg)
    {
        var errMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "assistant",
            Content = errorMsg
        };
        await _messageRepo.AddAsync(errMessage);
        await _messageRepo.SaveChangesAsync();
        return new ChatResponseDto { SessionId = sessionGuid, Answer = errorMsg, Sources = new List<string>() };
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
}