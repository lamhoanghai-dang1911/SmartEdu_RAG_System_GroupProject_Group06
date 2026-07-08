using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> AskAsync(ChatRequestDto request);
        Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionIds, string userId);
        Task<IEnumerable<ChatSessionDto>> GetSessionsByUserIdAsync(string userId);
        Task DeleteSessionAsync(string sessionId, string userId);
        Task<ChatResponseDto> ProcessChatWithBillingAsync(ChatRequestDto request);
        Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);

    }
}
