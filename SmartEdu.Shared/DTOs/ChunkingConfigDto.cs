using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class ChunkingConfigDto
    {
        public int Id { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public ChunkingStrategy Strategy { get; set; }
        public ChunkingScope Scope { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedByUserName { get; set; } = string.Empty;
    }

    public class ChunkingConfigSaveDto
    {
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public ChunkingStrategy Strategy { get; set; }
        public ChunkingScope Scope { get; set; }
        public int? SubjectId { get; set; }
    }
}
