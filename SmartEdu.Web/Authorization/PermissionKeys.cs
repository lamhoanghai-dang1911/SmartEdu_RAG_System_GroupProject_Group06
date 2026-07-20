namespace SmartEdu.Web.Authorization
{
    /// <summary>
    /// Central place for permission/role/section keys used by the UI.
    /// These keys mirror the existing authorization model (roles + IPermissionService)
    /// and are used by Tag Helpers, View Components, and Razor views.
    ///
    /// IMPORTANT: This file only contains identifiers for the UI to query.
    /// No business rules are added here. The Business layer remains the source of truth.
    /// </summary>
    public static class PermissionKeys
    {
        // ===== Role identifiers (must match SmartEdu.Shared.Enums.UserRole) =====
        public const string RoleAdmin = "Admin";
        public const string RoleLecturer = "Lecturer";
        public const string RoleStudent = "Student";

        // ===== Capability keys (semantic names that the UI hides/shows by) =====
        // These are NOT new permissions on the Business layer; they describe how
        // existing role + subject-scope checks map to user-visible features.

        // Subject management
        public const string CanManageSubjects = "subject.manage";          // Admin only
        public const string CanViewSubjectsAsLecturer = "subject.lecturer"; // Lecturer or Admin
        public const string CanImportStudents = "subject.import";         // Admin / assigned Lecturer

        // Lecturer management
        public const string CanManageLecturers = "lecturer.manage";       // Admin only

        // Document management
        public const string CanViewDocuments = "document.view";           // Any authenticated user (scoped)
        public const string CanUploadDocuments = "document.upload";       // Lecturer (leader) / Admin
        public const string CanEditDocuments = "document.edit";          // Lecturer / Admin
        public const string CanDeleteDocuments = "document.delete";      // Lecturer / Admin
        public const string CanTriggerEmbedding = "document.embed";      // Lecturer (leader of subject) / Admin

        // User / account management
        public const string CanManageUsers = "user.manage";               // Admin only
        public const string CanChangeOwnPassword = "user.password";       // Any authenticated

        // Chat
        public const string CanChat = "chat.use";                        // Student / Lecturer / Admin
        public const string CanViewOwnReports = "report.student";        // Student
        public const string CanViewAdminReports = "report.admin";        // Admin

        // Packages
        public const string CanViewPackageList = "package.list";          // Any user (AllowAnonymous)
        public const string CanBuyPackage = "package.buy";                // Student only
        public const string CanViewOwnSubscription = "package.own";      // Authenticated

        // System configuration (Admin only)
        public const string CanManageChunkingConfig = "config.chunking"; // Admin
        public const string CanManageUploadConfig = "config.upload";      // Admin
        public const string CanManageFreeTier = "config.freetier";        // Admin
    }
}