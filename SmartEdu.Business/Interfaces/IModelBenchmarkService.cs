using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartEdu.Shared.DTOs;
namespace SmartEdu.Business.Interfaces
{
    public interface IModelBenchmarkService
    {
        Task<IReadOnlyList<EmbeddingBenchmarkResultDto>>
       CompareEmbeddingModelsAsync(EmbeddingBenchmarkRequestDto request);

        Task<IReadOnlyList<ChatModelBenchmarkResultDto>>
            CompareChatModelsAsync(ChatModelBenchmarkRequestDto request);
    }
}
