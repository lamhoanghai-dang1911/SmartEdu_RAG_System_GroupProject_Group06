using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class ChatModelBenchmarkRequestDto
    {
        public int SubjectId { get; set; }

        public string Question { get; set; } = string.Empty;

        public int TopK { get; set; } = 5;

        public string EmbeddingModel { get; set; }
            = "intfloat/multilingual-e5-base";

        public List<ChatBenchmarkTargetDto> Targets { get; set; } = new()
    {
        new ChatBenchmarkTargetDto
        {
            Provider = "Gemini",
            Model = "gemini-2.5-flash"
        },
        new ChatBenchmarkTargetDto
        {
            Provider = "Groq",
            Model = "llama-3.3-70b-versatile"
        },
        new ChatBenchmarkTargetDto
        {
            Provider = "OpenRouter",
            Model = "deepseek/deepseek-chat"
        }
    };

        public List<string> ExpectedKeywords { get; set; } = new();

        public double Temperature { get; set; } = 0.3;

        public int MaxOutputTokens { get; set; } = 500;
    }
}
