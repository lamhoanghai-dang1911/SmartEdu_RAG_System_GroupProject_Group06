using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class DuplicateCheckDto
    {
        public bool HasDuplicate { get; set; }
        public int? DuplicateDocumentId { get; set; }
        public string? DuplicateTitle { get; set; }
        public DateTime? DuplicateCreatedAt { get; set; }
        // True if the existing document already has embeddings / is Ready
        public bool IsEmbeddingReady { get; set; }
    }

    public class DuplicateHandleDto
    {
        public int NewDocumentId { get; set; }
        public int OldDocumentId { get; set; }
        public DocumentDuplicateAction Action { get; set; } // Ignored = 1, Replaced = 2, KeptBoth = 3
    }
}
