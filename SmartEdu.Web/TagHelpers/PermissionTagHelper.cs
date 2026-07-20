using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SmartEdu.Web.Authorization;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.TagHelpers
{
    /// <summary>
    /// Conditionally renders its content based on a named capability key (see <see cref="PermissionKeys"/>).
    ///
    /// Usage:
    ///   &lt;permission capability="CanManageSubjects"&gt;...&lt;/permission&gt;
    ///   &lt;permission capability="CanUploadDocuments" exclude="true"&gt;...&lt;/permission&gt;
    ///
    /// The mapping from capability → underlying role/subject check lives in
    /// <see cref="UiAuthorizationExtensions"/>. No new business permissions are introduced.
    /// </summary>
    [HtmlTargetElement("permission", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class PermissionTagHelper : TagHelper
    {
        /// <summary>Capability name. Allowed values: see <see cref="PermissionKeys"/>.</summary>
        [HtmlAttributeName("capability")]
        public string Capability { get; set; } = string.Empty;

        /// <summary>Subject id used by subject-scoped capabilities (e.g. "CanTriggerEmbedding").</summary>
        [HtmlAttributeName("subject-id")]
        public int? SubjectId { get; set; }

        /// <summary>If true, inverts the check (renders when the user does NOT have the capability).</summary>
        [HtmlAttributeName("exclude")]
        public bool Exclude { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var user = ViewContext?.HttpContext?.User;
            bool allowed = Evaluate(user);
            if (allowed == Exclude)
            {
                output.SuppressOutput();
                return;
            }
            output.TagName = null;
        }

        private bool Evaluate(System.Security.Claims.ClaimsPrincipal? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(Capability)) return false;

            switch (Capability)
            {
                case PermissionKeys.CanManageSubjects:
                    return user.CanManageSubjects();
                case PermissionKeys.CanViewSubjectsAsLecturer:
                    return user.CanViewSubjectsAsLecturer();
                case PermissionKeys.CanImportStudents:
                    return user.CanImportStudents();
                case PermissionKeys.CanManageLecturers:
                    return user.CanManageLecturers();
                case PermissionKeys.CanViewDocuments:
                    return user.CanViewDocuments();
                case PermissionKeys.CanUploadDocuments:
                    return user.CanUploadDocuments();
                case PermissionKeys.CanEditDocuments:
                    return user.CanEditDocuments();
                case PermissionKeys.CanDeleteDocuments:
                    return user.CanDeleteDocuments();
                case PermissionKeys.CanManageUsers:
                    return user.CanManageUsers();
                case PermissionKeys.CanChangeOwnPassword:
                    return user.CanChangeOwnPassword();
                case PermissionKeys.CanChat:
                    return user.CanChat();
                case PermissionKeys.CanViewOwnReports:
                    return user.CanViewOwnReports();
                case PermissionKeys.CanViewAdminReports:
                    return user.CanViewAdminReports();
                case PermissionKeys.CanBuyPackage:
                    return user.CanBuyPackage();
                case PermissionKeys.CanViewOwnSubscription:
                    return user.CanViewOwnSubscription();
                case PermissionKeys.CanManageChunkingConfig:
                    return user.CanManageChunkingConfig();
                case PermissionKeys.CanManageUploadConfig:
                    return user.CanManageUploadConfig();
                case PermissionKeys.CanManageFreeTier:
                    return user.CanManageFreeTier();
                case PermissionKeys.CanTriggerEmbedding:
                    // Default safe: only admin/staff. Subject-level leader check happens in the controller/view.
                    return user.CanUploadDocuments();
                default:
                    return false;
            }
        }
    }
}