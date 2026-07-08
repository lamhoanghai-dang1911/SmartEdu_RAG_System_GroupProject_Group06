using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class EmbeddingSet : BaseEntity
    {
        public string FileHash { get; set; } = string.Empty;

        public int ChunkingConfigId { get; set; }
        public ChunkingConfig ChunkingConfig { get; set; } = null!;

        public string EmbeddingModel { get; set; } = string.Empty; // "gemini-embedding", "e5", ...
        public EmbeddingSetStatus Status { get; set; } = EmbeddingSetStatus.Pending;

        // Document đầu tiên tạo ra set này (phục vụ audit, không phải chủ sở hữu độc quyền)
        public int SourceDocumentId { get; set; }
        public Document SourceDocument { get; set; } = null!;

        public string CanonicalTitle { get; set; } = string.Empty;
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
        public ICollection<Document> Documents { get; set; } = new List<Document>(); // tất cả Document dùng chung set này
    }
}
