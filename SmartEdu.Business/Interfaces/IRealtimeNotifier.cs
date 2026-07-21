using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task SendDocumentLogAsync(int documentId, string message, string status);
        Task SendDocumentProcessingFinishedAsync(int documentId, string status);

        Task SendPaymentCompletedAsync(int userId, int orderId, string packageName, int remainingTokenQuota, DateTime endDate);
        Task SendTokenQuotaUpdatedAsync(int userId, int remainingTokenQuota);

        Task SendChatMessageAsync(string sessionId, string role, string content, object? citations, string? excludeConnectionId = null);
        Task SendDocumentCreatedAsync(DocumentDto document);
        Task SendDocumentDeletedAsync(int documentId, int subjectId);
        Task SendDocumentUpdatedAsync(DocumentDto document);
        Task SendDocumentStatusChangedAsync(int documentId, int subjectId, string status);
        Task SendSessionDeletedAsync(int userId, string sessionId);
        Task SendSessionUpsertAsync(int userId, ChatSessionDto session);

        Task SendSubjectCreatedAsync(SubjectDto subject);
        Task SendSubjectUpdatedAsync(SubjectDto subject, IEnumerable<int> affectedUserIds);
        Task SendSubjectDeletedAsync(int subjectId, IEnumerable<int> affectedUserIds);
        Task SendSubjectAssignedToLecturerAsync(int lecturerId, SubjectDto subject);
        Task SendSubjectUnassignedFromLecturerAsync(int lecturerId, int subjectId);
        Task SendChatProgressAsync(string sessionId, string stage, string message);
        Task SendReportsUpdatedAsync(int userId);

    }
}
