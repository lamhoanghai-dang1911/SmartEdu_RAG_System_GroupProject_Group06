using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class UsageLog : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public FeatureType Feature { get; set; }        // Chat | Chunking
        public string ModelUsed { get; set; } = string.Empty; // "gemini-pro", "e5", ...

        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;

        public int? ChatSessionId { get; set; }         // null nếu Feature = Chunking
        public int? DocumentId { get; set; }             // null nếu Feature = Chat
    }
}
