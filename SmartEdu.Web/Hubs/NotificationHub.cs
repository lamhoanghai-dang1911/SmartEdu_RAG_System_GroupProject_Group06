using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;

namespace SmartEdu.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
                    _logger?.LogDebug("Connection {ConnectionId} added to user group {Group}", Context.ConnectionId, UserGroup(userId));

                    // Robust admin detection: IsInRole plus any role-like claim types ("role", ClaimTypes.Role)
                    bool isAdmin = false;
                    try
                    {
                        if (Context.User?.IsInRole("Admin") == true) isAdmin = true;
                        if (!isAdmin && Context.User?.Identity is ClaimsIdentity ci)
                        {
                            var roleClaim = ci.Claims.FirstOrDefault(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
                                                                          || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase)
                                                                          || c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase));
                            if (roleClaim != null && string.Equals(roleClaim.Value, "Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                isAdmin = true;
                            }
                        }
                    }
                    catch (Exception exRole)
                    {
                        _logger?.LogWarning(exRole, "Error while detecting roles for connection {ConnectionId}", Context.ConnectionId);
                    }

                    if (isAdmin)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroupName);
                        _logger?.LogInformation("Connection {ConnectionId} added to admins group", Context.ConnectionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in NotificationHub.OnConnectedAsync");
            }

            await base.OnConnectedAsync();
        }

        public static string UserGroup(string userId) => $"user-{userId}";
        public static string UserGroup(int userId) => $"user-{userId}";
        public const string AdminsGroupName = "group-admins";
    }
}
