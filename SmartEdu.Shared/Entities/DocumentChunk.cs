namespace SmartEdu.Shared.Entities
{
    public class DocumentChunk : BaseEntity
    {
        // Đổi chủ sở hữu: thuộc EmbeddingSet, không thuộc Document nữa
        public int EmbeddingSetId { get; set; }
        public EmbeddingSet EmbeddingSet { get; set; } = null!;

        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? EmbeddingJson { get; set; }
        public string? SourceLocation { get; set; }
    }
}
