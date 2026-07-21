namespace SmartEdu.Shared.DTOs
{
    public class DocumentChunkDto
    {
        public int ChunkId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string? SourceLocation { get; set; }
        public int CharacterCount { get; set; }
        public int WordCount { get; set; }
        public int ConfiguredChunkSize { get; set; }
        public int ConfiguredOverlap { get; set; }
        public int ActualOverlap { get; set; }
        public string ChunkingStrategy { get; set; } = string.Empty;
    }
}
