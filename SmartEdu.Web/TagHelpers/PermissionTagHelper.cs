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

            switch (Capability.Trim())
            {
                case nameof(PermissionKeys.CanManageSubjects):
                case PermissionKeys.CanManageSubjects:
                    return user.CanManageSubjects();
                case nameof(PermissionKeys.CanViewSubjectsAsLecturer):
                case PermissionKeys.CanViewSubjectsAsLecturer:
                    return user.CanViewSubjectsAsLecturer();
                case nameof(PermissionKeys.CanImportStudents):
                case PermissionKeys.CanImportStudents:
                    return user.CanImportStudents();
                case nameof(PermissionKeys.CanManageLecturers):
                case PermissionKeys.CanManageLecturers:
                    return user.CanManageLecturers();
                case nameof(PermissionKeys.CanViewDocuments):
                case PermissionKeys.CanViewDocuments:
                    return user.CanViewDocuments();
                case nameof(PermissionKeys.CanUploadDocuments):
                case PermissionKeys.CanUploadDocuments:
                    return user.CanUploadDocuments();
                case nameof(PermissionKeys.CanEditDocuments):
                case PermissionKeys.CanEditDocuments:
                    return user.CanEditDocuments();
                case nameof(PermissionKeys.CanDeleteDocuments):
                case PermissionKeys.CanDeleteDocuments:
                    return user.CanDeleteDocuments();
                case nameof(PermissionKeys.CanManageUsers):
                case PermissionKeys.CanManageUsers:
                    return user.CanManageUsers();
                case nameof(PermissionKeys.CanChangeOwnPassword):
                case PermissionKeys.CanChangeOwnPassword:
                    return user.CanChangeOwnPassword();
                case nameof(PermissionKeys.CanChat):
                case PermissionKeys.CanChat:
                    return user.CanChat();
                case nameof(PermissionKeys.CanViewOwnReports):
                case PermissionKeys.CanViewOwnReports:
                    return user.CanViewOwnReports();
                case nameof(PermissionKeys.CanViewAdminReports):
                case PermissionKeys.CanViewAdminReports:
                    return user.CanViewAdminReports();
                case nameof(PermissionKeys.CanBuyPackage):
                case PermissionKeys.CanBuyPackage:
                    return user.CanBuyPackage();
                case nameof(PermissionKeys.CanViewOwnSubscription):
                case PermissionKeys.CanViewOwnSubscription:
                    return user.CanViewOwnSubscription();
                case nameof(PermissionKeys.CanManageChunkingConfig):
                case PermissionKeys.CanManageChunkingConfig:
                    return user.CanManageChunkingConfig();
                case nameof(PermissionKeys.CanManageUploadConfig):
                case PermissionKeys.CanManageUploadConfig:
                    return user.CanManageUploadConfig();
                case nameof(PermissionKeys.CanManageFreeTier):
                case PermissionKeys.CanManageFreeTier:
                    return user.CanManageFreeTier();
                case nameof(PermissionKeys.CanTriggerEmbedding):
                case PermissionKeys.CanTriggerEmbedding:
                    // Default safe: only admin/staff. Subject-level leader check happens in the controller/view.
                    return user.CanUploadDocuments();
                default:
                    return false;
            }
        }
    }
}
