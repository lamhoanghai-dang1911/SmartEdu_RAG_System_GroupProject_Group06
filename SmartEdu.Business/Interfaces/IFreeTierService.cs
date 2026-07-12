using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IFreeTierService
    {
        Task<int> GetRemainingFreeTokensAsync(int userId);
        Task DeductFreeTokensAsync(int userId, int tokensUsed);
        Task<IEnumerable<FreeTierConfigDto>> GetAllConfigsAsync();
        Task CreateConfigAsync(FreeTierConfigSaveDto dto, int userId);
    }
}
