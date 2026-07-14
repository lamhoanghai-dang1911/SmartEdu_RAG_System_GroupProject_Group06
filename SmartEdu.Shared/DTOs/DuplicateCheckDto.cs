using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public enum DuplicateMatchType { None = 0, Exact = 1, Near = 2 }

    public class DuplicateCheckDto
    {
        public bool HasDuplicate { get; set; }
        public DuplicateMatchType MatchType { get; set; } = DuplicateMatchType.None;
        public int? DuplicateDocumentId { get; set; }
        public string? DuplicateTitle { get; set; }
        public DateTime? DuplicateCreatedAt { get; set; }
        public bool IsEmbeddingReady { get; set; }
        public double? SimilarityPercent { get; set; }   // chỉ có giá trị khi MatchType == Near
    }

    public class DuplicateHandleDto
    {
        public int NewDocumentId { get; set; }
        public int OldDocumentId { get; set; }
        public DocumentDuplicateAction Action { get; set; }
    }
}
