namespace SmartEdu.Shared.DTOs
{
    public class DocumentChunkDetailDto
    {
        public int ChunkId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public int EmbeddingSetId { get; set; }
        public string? SourceLocation { get; set; }   // ← thêm
        public List<DocumentShortDto> Documents { get; set; } = new List<DocumentShortDto>();
    }

    public class DocumentShortDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}