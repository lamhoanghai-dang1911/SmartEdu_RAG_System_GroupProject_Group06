using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SmartEdu.Web.Authorization;

namespace SmartEdu.Web.TagHelpers
{
    /// <summary>
    /// Renders its content only when the current user is in the specified role(s).
    ///
    /// Usage:
    ///   &lt;role name="Admin"&gt;...&lt;/role&gt;
    ///   &lt;role roles="Admin,Lecturer"&gt;...&lt;/role&gt;
    ///   &lt;role name="Admin" exclude="true"&gt;...&lt;/role&gt;   (renders when NOT admin)
    /// </summary>
    [HtmlTargetElement("role", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class RoleTagHelper : TagHelper
    {
        [HtmlAttributeName("name")]
        public string? Name { get; set; }

        [HtmlAttributeName("roles")]
        public string? Roles { get; set; }

        [HtmlAttributeName("exclude")]
        public bool Exclude { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext? ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var user = ViewContext?.HttpContext?.User;
            bool allowed = Evaluate(user);
            if (allowed == Exclude)
            {
                output.SuppressOutput();
                return;
            }
            // Strip the wrapper element so only the inner content is emitted.
            output.TagName = null;
        }

        private bool Evaluate(System.Security.Claims.ClaimsPrincipal? user)
        {
            if (user == null) return false;
            var names = Split();
            if (names.Length == 0) return false;
            foreach (var n in names)
            {
                if (user.IsInRole(n)) return true;
            }
            return false;
        }

        private string[] Split()
        {
            if (!string.IsNullOrWhiteSpace(Roles))
            {
                return Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return new[] { Name };
            }
            return Array.Empty<string>();
        }
    }
}