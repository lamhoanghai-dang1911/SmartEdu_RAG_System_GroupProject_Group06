using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class ModelComparisonLog : BaseEntity
    {
        public FeatureType Feature { get; set; }         // Chat | Chunking
        public string TestInput { get; set; } = string.Empty;   // câu hỏi test / đoạn văn test

        public string ModelA { get; set; } = string.Empty;
        public string ResponseA { get; set; } = string.Empty;
        public int LatencyAMs { get; set; }
        public int TokensA { get; set; }

        public string ModelB { get; set; } = string.Empty;
        public string ResponseB { get; set; } = string.Empty;
        public int LatencyBMs { get; set; }
        public int TokensB { get; set; }

        public string? WinnerModel { get; set; }          // chấm thủ công hoặc LLM giám khảo
        public string? EvaluationNote { get; set; }
    }
}
