using System.Reflection.Metadata;

namespace SmartEdu.Shared.Entities
{
    public class Subject : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
