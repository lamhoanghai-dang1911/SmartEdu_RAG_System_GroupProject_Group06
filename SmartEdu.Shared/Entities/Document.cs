using SmartEdu.Shared.Enums;

namespace SmartEdu.Shared.Entities
{
    public class Document : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;   // SHA256 nội dung file

        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        // Versioning trong cùng Subject (Ignore/Replace/KeepBoth)
        public int? Version { get; set; }
        public int? ParentDocumentId { get; set; }
        public Document? ParentDocument { get; set; }

        // Trỏ tới EmbeddingSet — null nếu đang chờ xử lý lần đầu
        public int? EmbeddingSetId { get; set; }
        public EmbeddingSet? EmbeddingSet { get; set; }

        public ICollection<DocumentLog> Logs { get; set; } = new List<DocumentLog>();
        public DocumentDuplicateAction? DuplicateAction { get; set; }
    }
}
