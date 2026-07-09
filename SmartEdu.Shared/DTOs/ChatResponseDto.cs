namespace SmartEdu.Shared.DTOs
{
    public class ChatResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new List<string>();
        public List<CitationDto> Citations { get; set; } = new List<CitationDto>();
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
        public int? RemainingTokenQuota { get; set; }
    }

    public class CitationDto
    {
        public int Number { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int ChunkId { get; set; }
        public int DocumentId { get; set; }        // ← thêm
    }
}
