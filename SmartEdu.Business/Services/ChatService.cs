using Microsoft.EntityFrameworkCore;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Business.Services
{
    public class ChatService : IChatService
    {
        private readonly IRepository<ChatSession> _sessionRepo;
        private readonly IRepository<ChatMessage> _messageRepo;
        private readonly AppDbContext _context;

        public ChatService(
            IRepository<ChatSession> sessionRepo,
            IRepository<ChatMessage> messageRepo,
            AppDbContext context)
        {
            _sessionRepo = sessionRepo;
            _messageRepo = messageRepo;
            _context = context;
        }

        public async Task<ChatResponseDto> AskAsync(ChatRequestDto request)
        {
            // Tìm hoặc tạo session
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

            // Lưu câu hỏi user
            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "user",
                Content = request.Question
            };
            await _messageRepo.AddAsync(userMessage);
            await _messageRepo.SaveChangesAsync();

            // TODO: Gọi RAG pipeline thực sự ở ASM2/Final
            var answerText = $"[RAG chưa tích hợp] Câu hỏi: \"{request.Question}\" " +
                             $"sẽ được trả lời sau khi tích hợp Embedding + Vector Search.";

            // Lưu câu trả lời assistant
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
                Sources = new List<string>()
            };
        }

        public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId)
        {
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
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
                })
                .ToListAsync();
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
                    SubjectName = s.Subject != null ? s.Subject.Name : "Tất cả",
                    MessageCount = s.Messages.Count,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session is null) return;

            session.IsDeleted = true;
            session.UpdatedAt = DateTime.UtcNow;
            _sessionRepo.Update(session);
            await _sessionRepo.SaveChangesAsync();
        }
    }
}
