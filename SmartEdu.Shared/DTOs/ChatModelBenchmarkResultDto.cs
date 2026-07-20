using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class ChatModelBenchmarkResultDto
    {
        public string Provider { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public long ElapsedMs { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public int TotalTokens => PromptTokens + CompletionTokens;

        public double KeywordCoveragePercent { get; set; }

        public double GroundednessPercent { get; set; }

        public double RelevancePercent { get; set; }

        public double CompletenessPercent { get; set; }

        public double HallucinationPercent { get; set; }

        public double TokensPerSecond { get; set; }

        public double OverallScore { get; set; }

        public string Answer { get; set; } = string.Empty;

        public string RetrievedContext { get; set; } = string.Empty;

        public List<BenchmarkChunkDto> RetrievedChunks { get; set; } = new();

        public string? Error { get; set; }
    }
}
