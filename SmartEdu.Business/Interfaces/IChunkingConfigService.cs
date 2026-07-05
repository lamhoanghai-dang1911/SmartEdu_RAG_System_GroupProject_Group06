using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IChunkingConfigService
    {
        Task<IEnumerable<ChunkingConfigDto>> GetAllAsync();
        Task<ChunkingConfigDto?> GetByIdAsync(int id);
        Task SaveAsync(ChunkingConfigSaveDto dto, int updatedByUserId);

        // Dùng nội bộ bởi DocumentService khi upload/embed
        Task<ChunkingConfig> ResolveActiveConfigAsync(int subjectId);
    }
}
