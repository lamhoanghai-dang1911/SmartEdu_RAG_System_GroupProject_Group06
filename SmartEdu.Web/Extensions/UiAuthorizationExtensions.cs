using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Web.Authorization;

namespace SmartEdu.Web.Extensions
{
    /// <summary>
    /// UI-side permission helpers that wrap the existing role + IPermissionService checks.
    /// These helpers are intentionally thin so the Business layer remains the source of truth.
    /// </summary>
    public static class UiAuthorizationExtensions
    {
        // ===== Role shortcuts =====
        public static bool UiIsAdmin(this ClaimsPrincipal user) => user?.IsInRole(PermissionKeys.RoleAdmin) ?? false;
        public static bool UiIsLecturer(this ClaimsPrincipal user) => user?.IsInRole(PermissionKeys.RoleLecturer) ?? false;
        public static bool UiIsStudent(this ClaimsPrincipal user) => user?.IsInRole(PermissionKeys.RoleStudent) ?? false;
        public static bool UiIsStaff(this ClaimsPrincipal user) => user.UiIsAdmin() || user.UiIsLecturer();

        // ===== Capability checks (mirror what controllers do) =====

        public static bool CanManageSubjects(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanViewSubjectsAsLecturer(this ClaimsPrincipal user)
            => user.UiIsAdmin() || user.UiIsLecturer();

        public static bool CanImportStudents(this ClaimsPrincipal user)
            => user.UiIsAdmin() || user.UiIsLecturer();

        public static bool CanManageLecturers(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanViewDocuments(this ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated ?? false;

        public static bool CanUploadDocuments(this ClaimsPrincipal user)
            => user.UiIsAdmin() || user.UiIsLecturer();

        public static bool CanEditDocuments(this ClaimsPrincipal user)
            => user.UiIsAdmin() || user.UiIsLecturer();

        public static bool CanDeleteDocuments(this ClaimsPrincipal user)
            => user.UiIsAdmin() || user.UiIsLecturer();

        /// <summary>
        /// Trigger embedding requires either Admin OR a Lecturer who is leader of the given subject.
        /// The leader check is performed asynchronously at the call site (see Document index).
        /// </summary>
        public static bool CanTriggerEmbeddingFor(this ClaimsPrincipal user, int subjectId,
            System.Collections.Generic.HashSet<int>? leaderSubjectIds = null)
        {
            if (user.UiIsAdmin()) return true;
            if (!user.UiIsLecturer()) return false;
            return leaderSubjectIds != null && leaderSubjectIds.Contains(subjectId);
        }

        public static bool CanManageUsers(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanChangeOwnPassword(this ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated ?? false;

        public static bool CanChat(this ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated ?? false;

        public static bool CanViewOwnReports(this ClaimsPrincipal user)
            => user.UiIsStudent();

        public static bool CanViewAdminReports(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanBuyPackage(this ClaimsPrincipal user)
            => user.UiIsStudent();

        public static bool CanViewOwnSubscription(this ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated ?? false;

        public static bool CanManageChunkingConfig(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanManageUploadConfig(this ClaimsPrincipal user)
            => user.UiIsAdmin();

        public static bool CanManageFreeTier(this ClaimsPrincipal user)
            => user.UiIsAdmin();
    }
}