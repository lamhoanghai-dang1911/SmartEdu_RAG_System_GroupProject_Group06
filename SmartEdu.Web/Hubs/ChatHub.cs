using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartEdu.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        }

        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        }

        public static string SessionGroup(string sessionId) => $"chat-session-{sessionId}";
    }
}
