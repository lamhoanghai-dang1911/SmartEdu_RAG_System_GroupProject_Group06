using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartEdu.Web.Hubs
{
    [Authorize]
    public class DocumentProcessingHub : Hub
    {
        public async Task JoinDocumentGroup(int documentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(documentId));
        }

        public async Task LeaveDocumentGroup(int documentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(documentId));
        }

        public async Task JoinDocumentListGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ListGroupName);
        }

        public async Task LeaveDocumentListGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ListGroupName);
        }

        public const string ListGroupName = "document-list";

        public static string GroupName(int documentId) => $"document-{documentId}";
    }
}
