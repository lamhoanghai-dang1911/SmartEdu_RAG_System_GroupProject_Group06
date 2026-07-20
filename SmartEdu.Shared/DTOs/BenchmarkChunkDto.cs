using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class BenchmarkChunkDto
    {
        public int ChunkId { get; set; }

        public int ChunkIndex { get; set; }

        public string DocumentTitle { get; set; } = string.Empty;

        public string ContentPreview { get; set; } = string.Empty;

        public double Score { get; set; }
    }
}
