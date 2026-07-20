using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class EmbeddingBenchmarkRequestDto
    {
        public int SubjectId { get; set; }

        public string Query { get; set; } = string.Empty;

        public List<string> Models { get; set; } = new()
    {
        "intfloat/multilingual-e5-base",
        "sentence-transformers/paraphrase-multilingual-mpnet-base-v2"
    };

        public int CandidateLimit { get; set; } = 20;

        public int TopK { get; set; } = 5;

        // Danh sách chunk được xem là kết quả đúng.
        // Có thể để trống nếu chưa có dữ liệu đánh giá.
        public List<int> ExpectedChunkIds { get; set; } = new();
    }
}
