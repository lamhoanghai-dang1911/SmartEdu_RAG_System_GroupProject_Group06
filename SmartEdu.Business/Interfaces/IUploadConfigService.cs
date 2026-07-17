using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IUploadConfigService
    {
        Task<IEnumerable<UploadConfigDto>> GetAllAsync();
        Task<int> ResolveMaxFileSizeMBAsync(int subjectId, string fileType);
        Task CreateAsync(UploadConfigSaveDto dto, int userId);
        Task<double> ResolveNearDuplicateThresholdAsync();
    }
}
