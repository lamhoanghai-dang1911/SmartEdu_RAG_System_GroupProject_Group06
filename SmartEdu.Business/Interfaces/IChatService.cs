using SmartEdu.Shared.DTOs;

namespace SmartEdu.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> AskAsync(ChatRequestDto request);
        Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId);
        Task<IEnumerable<ChatSessionDto>> GetAllSessionsAsync();
        Task DeleteSessionAsync(string sessionId);
    }
}
