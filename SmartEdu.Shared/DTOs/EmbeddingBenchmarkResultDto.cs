using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class EmbeddingBenchmarkResultDto
    {
        public string Model { get; set; } = string.Empty;

        public long ElapsedMs { get; set; }

        public int VectorDimensions { get; set; }

        public double TopScore { get; set; }

        public double AverageTopKScore { get; set; }

        public double? RecallAtK { get; set; }

        public List<BenchmarkChunkDto> TopChunks { get; set; } = new();

        public string? Error { get; set; }
    }
}
