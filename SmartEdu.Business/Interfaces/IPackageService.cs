using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IPackageService
    {
        Task<IEnumerable<PackageDto>> GetActivePackagesAsync();
        Task<PackageDto?> GetByIdAsync(int packageId);
    }
}
