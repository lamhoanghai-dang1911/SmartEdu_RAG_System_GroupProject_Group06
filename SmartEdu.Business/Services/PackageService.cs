using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Services
{
    public class PackageService : IPackageService
    {
        private readonly IRepository<Package> _packageRepo;

        public PackageService(IRepository<Package> packageRepo)
        {
            _packageRepo = packageRepo;
        }

        public async Task<IEnumerable<PackageDto>> GetActivePackagesAsync()
        {
            var packages = await _packageRepo.GetAllAsync(p => p.IsActive && !p.IsDeleted);

            return packages.Select(p => new PackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                TokenQuota = p.TokenQuota,
                IsActive = p.IsActive
            }).ToList();
        }

        public async Task<PackageDto?> GetByIdAsync(int packageId)
        {
            var package = await _packageRepo.GetByIdAsync(packageId);
            if (package == null || !package.IsActive || package.IsDeleted) return null;

            return new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                TokenQuota = package.TokenQuota,
                IsActive = package.IsActive
            };
        }
    }
}
