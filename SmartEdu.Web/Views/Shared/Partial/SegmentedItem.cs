namespace SmartEdu.Web.Views.Shared
{
    public class SegmentedItem
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Href { get; set; }
        public bool Active { get; set; }
    }
}