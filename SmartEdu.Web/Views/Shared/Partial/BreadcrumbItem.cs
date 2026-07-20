namespace SmartEdu.Web.Views.Shared
{
    /// <summary>Plain DTO used by the _Breadcrumb partial.</summary>
    public class BreadcrumbItem
    {
        public string Label { get; set; } = string.Empty;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public object? RouteValues { get; set; }
        public bool Active { get; set; }
    }
}