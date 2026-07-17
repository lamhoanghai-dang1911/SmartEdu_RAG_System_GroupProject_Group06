using Microsoft.AspNetCore.SignalR;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Web.Hubs;

namespace SmartEdu.Web.Realtime
{
    public class SignalRNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<DocumentProcessingHub> _docHub;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IHubContext<NotificationHub> _notifyHub;

        public SignalRNotifier(
            IHubContext<DocumentProcessingHub> docHub,
            IHubContext<ChatHub> chatHub,
            IHubContext<NotificationHub> notifyHub)
        {
            _docHub = docHub;
            _chatHub = chatHub;
            _notifyHub = notifyHub;
        }

        public async Task SendDocumentLogAsync(int documentId, string message, string status)
        {
            await _docHub.Clients.Group(DocumentProcessingHub.GroupName(documentId))
                .SendAsync("ReceiveLog", new { message, status, timestamp = DateTime.UtcNow });
        }

        public async Task SendDocumentProcessingFinishedAsync(int documentId, string status)
        {
            await _docHub.Clients.Group(DocumentProcessingHub.GroupName(documentId))
                .SendAsync("ProcessingFinished", new { status });
        }

        public async Task SendPaymentCompletedAsync(int userId, int orderId, string packageName, int remainingTokenQuota, DateTime endDate)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("PaymentCompleted", new { orderId, packageName, remainingTokenQuota, endDate });
        }

        public async Task SendTokenQuotaUpdatedAsync(int userId, int remainingTokenQuota)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("TokenQuotaUpdated", new { remainingTokenQuota });
        }

        public async Task SendChatMessageAsync(string sessionId, string role, string content, object? citations, string? excludeConnectionId = null)
        {
            var group = _chatHub.Clients.Group(ChatHub.SessionGroup(sessionId));
            var target = string.IsNullOrEmpty(excludeConnectionId)
                ? group
                : _chatHub.Clients.GroupExcept(ChatHub.SessionGroup(sessionId), new[] { excludeConnectionId });

            await target.SendAsync("ReceiveMessage", new { role, content, citations });
        }

        public async Task SendDocumentCreatedAsync(DocumentDto document)
        {
            await _docHub.Clients.Group(DocumentProcessingHub.ListGroupName)
                .SendAsync("DocumentCreated", document);
        }

        public async Task SendDocumentDeletedAsync(int documentId, int subjectId)
        {
            await _docHub.Clients.Group(DocumentProcessingHub.ListGroupName)
                .SendAsync("DocumentDeleted", new { documentId, subjectId });
        }

        public async Task SendDocumentStatusChangedAsync(int documentId, int subjectId, string status)
        {
            await _docHub.Clients.Group(DocumentProcessingHub.ListGroupName)
                .SendAsync("DocumentStatusChanged", new { documentId, subjectId, status });
        }

        public async Task SendSessionDeletedAsync(int userId, string sessionId)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("SessionDeleted", new { sessionId });
        }

        public async Task SendSessionUpsertAsync(int userId, ChatSessionDto session)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("SessionUpsert", session);
        }

        public async Task SendSubjectCreatedAsync(SubjectDto subject)
        {
            // Broadcast cho các Admin khác đang mở trang Subject/Index (nếu muốn dùng nhóm Admin riêng, xem ghi chú)
            await _notifyHub.Clients.All.SendAsync("SubjectCreated", subject);
        }

        public async Task SendSubjectUpdatedAsync(SubjectDto subject, IEnumerable<int> affectedUserIds)
        {
            foreach (var userId in affectedUserIds.Distinct())
            {
                await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                    .SendAsync("SubjectUpdated", subject);
            }
        }

        public async Task SendSubjectDeletedAsync(int subjectId, IEnumerable<int> affectedUserIds)
        {
            foreach (var userId in affectedUserIds.Distinct())
            {
                await _notifyHub.Clients.Group(NotificationHub.UserGroup(userId))
                    .SendAsync("SubjectDeleted", new { subjectId });
            }
        }

        public async Task SendSubjectAssignedToLecturerAsync(int lecturerId, SubjectDto subject)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(lecturerId))
                .SendAsync("SubjectAssigned", subject);
        }

        public async Task SendSubjectUnassignedFromLecturerAsync(int lecturerId, int subjectId)
        {
            await _notifyHub.Clients.Group(NotificationHub.UserGroup(lecturerId))
                .SendAsync("SubjectUnassigned", new { subjectId });
        }

        public async Task SendDocumentUpdatedAsync(DocumentDto document)
        {
            // Notify both the document list viewers and viewers of the specific document
            try
            {
                await _docHub.Clients.Group(DocumentProcessingHub.ListGroupName)
                    .SendAsync("DocumentUpdated", document);

                await _docHub.Clients.Group(DocumentProcessingHub.GroupName(document.Id))
                    .SendAsync("DocumentUpdated", document);
            }
            catch
            {
                // swallow to avoid affecting caller; logging can be added if needed
            }
        }

        public async Task SendChatProgressAsync(string sessionId, string stage, string message)
        {
            await _chatHub.Clients.Group(ChatHub.SessionGroup(sessionId))
                .SendAsync("ReceiveProgress", new { stage, message });
        }
    }
}
