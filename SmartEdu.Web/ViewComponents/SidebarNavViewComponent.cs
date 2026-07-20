using Microsoft.AspNetCore.Mvc;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.ViewComponents
{
    /// <summary>
    /// View Component that renders the sidebar navigation, automatically filtering items
    /// out based on the current user's capabilities. No business permissions are added;
    /// each item is gated by <see cref="UiAuthorizationExtensions"/>.
    /// </summary>
    public class SidebarNavViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string? activeKey = null)
        {
            var user = HttpContext.User;
            var items = SidebarItem.BuildAll(user);
            ViewData["ActiveKey"] = activeKey;
            return View(items);
        }
    }

    public class SidebarSection
    {
        public string Title { get; set; } = string.Empty;
        public List<SidebarItem> Items { get; } = new();
    }

    public class SidebarItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public bool Allowed { get; set; }

        /// <summary>
        /// Returns true if any of the supplied route data matches this item, so the view
        /// can highlight the current page.
        /// </summary>
        public bool IsActive(string? area, string controller, string action)
        {
            if (!string.Equals(controller, Controller, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(action ?? "Index", Action ?? "Index", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrEmpty(Area)) return true;
            return string.Equals(area, Area, StringComparison.OrdinalIgnoreCase);
        }

        public static List<SidebarSection> BuildAll(System.Security.Claims.ClaimsPrincipal user)
        {
            var sections = new List<SidebarSection>();

            // ===== Overview / main =====
            var main = new SidebarSection { Title = "Overview" };
            TryAdd(main, new SidebarItem
            {
                Key = "dashboard",
                Title = "Dashboard",
                Icon = "bi-speedometer2",
                Controller = "Home",
                Action = "Index",
                Allowed = user.Identity?.IsAuthenticated ?? false
            });
            TryAdd(main, new SidebarItem
            {
                Key = "chat",
                Title = "AI Chatbot",
                Icon = "bi-chat-dots",
                Controller = "Chat",
                Action = "Index",
                Allowed = user.CanChat()
            });
            if (main.Items.Count > 0) sections.Add(main);

            // ===== Learning =====
            var learning = new SidebarSection { Title = "Learning" };
            TryAdd(learning, new SidebarItem
            {
                Key = "subjects",
                Title = "Subjects",
                Icon = "bi-book",
                Controller = "Subject",
                Action = "Index",
                Allowed = user.CanViewSubjectsAsLecturer() || user.UiIsStudent()
            });
            TryAdd(learning, new SidebarItem
            {
                Key = "documents",
                Title = "Documents",
                Icon = "bi-file-earmark-text",
                Controller = "Document",
                Action = "Index",
                Allowed = user.CanViewDocuments()
            });
            if (learning.Items.Count > 0) sections.Add(learning);

            // ===== Subscription / payments =====
            var billing = new SidebarSection { Title = "Subscription" };
            TryAdd(billing, new SidebarItem
            {
                Key = "packages",
                Title = "Pricing",
                Icon = "bi-stars",
                Controller = "Package",
                Action = "List",
                Allowed = true
            });
            TryAdd(billing, new SidebarItem
            {
                Key = "subscription",
                Title = "My Subscription",
                Icon = "bi-bag-check",
                Controller = "Package",
                Action = "MySubscription",
                Allowed = user.CanViewOwnSubscription()
            });
            if (billing.Items.Count > 0) sections.Add(billing);

            // ===== Reports =====
            var reports = new SidebarSection { Title = "Reports" };
            TryAdd(reports, new SidebarItem
            {
                Key = "student-reports",
                Title = "My Usage",
                Icon = "bi-graph-up",
                Controller = "StudentReports",
                Action = "Index",
                Allowed = user.CanViewOwnReports()
            });
            TryAdd(reports, new SidebarItem
            {
                Key = "admin-reports",
                Title = "Admin Reports",
                Icon = "bi-bar-chart-line",
                Controller = "Reports",
                Action = "Index",
                Allowed = user.CanViewAdminReports()
            });
            if (reports.Items.Count > 0) sections.Add(reports);

            // ===== Administration =====
            var admin = new SidebarSection { Title = "Administration" };
            TryAdd(admin, new SidebarItem
            {
                Key = "lecturers",
                Title = "Lecturers",
                Icon = "bi-person-badge",
                Controller = "LecturerManagement",
                Action = "Index",
                Allowed = user.CanManageLecturers()
            });
            TryAdd(admin, new SidebarItem
            {
                Key = "users",
                Title = "Users",
                Icon = "bi-people",
                Controller = "Account",
                Action = "ManageUsers",
                Allowed = user.CanManageUsers()
            });
            TryAdd(admin, new SidebarItem
            {
                Key = "model-benchmark",
                Title = "Model Benchmark",
                Icon = "bi-speedometer2",
                Controller = "ModelBenchmark",
                Action = "Index",
                Allowed = user.CanManageUsers()
            });
            TryAdd(admin, new SidebarItem
            {
                Key = "chunking",
                Title = "Chunking Config",
                Icon = "bi-diagram-3",
                Controller = "ChunkingConfig",
                Action = "Index",
                Allowed = user.CanManageChunkingConfig()
            });
            TryAdd(admin, new SidebarItem
            {
                Key = "upload",
                Title = "Upload Config",
                Icon = "bi-cloud-arrow-up",
                Controller = "UploadConfig",
                Action = "Index",
                Allowed = user.CanManageUploadConfig()
            });
            TryAdd(admin, new SidebarItem
            {
                Key = "freetier",
                Title = "Free Tier",
                Icon = "bi-currency-dollar",
                Controller = "FreeTierConfig",
                Action = "Index",
                Allowed = user.CanManageFreeTier()
            });
            if (admin.Items.Count > 0) sections.Add(admin);

            // Filter out any sections that ended up empty
            sections.RemoveAll(s => s.Items.Count == 0);
            return sections;
        }

        private static void TryAdd(SidebarSection section, SidebarItem item)
        {
            if (item.Allowed) section.Items.Add(item);
        }
    }
}
