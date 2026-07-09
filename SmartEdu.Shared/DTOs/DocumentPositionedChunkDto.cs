using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class DocumentPositionedChunkDto
    {
        public int ChunkId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? SourceLocation { get; set; }
    }

    public class DocumentSourcePanelDto
    {
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public List<DocumentPositionedChunkDto> Chunks { get; set; } = new List<DocumentPositionedChunkDto>();
    }
}
