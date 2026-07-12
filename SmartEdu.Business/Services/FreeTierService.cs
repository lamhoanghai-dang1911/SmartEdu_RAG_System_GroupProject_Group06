using Microsoft.Extensions.Caching.Memory;
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
    public class FreeTierService : IFreeTierService
    {
        private readonly IRepository<FreeTierConfig> _configRepo;
        private readonly IRepository<FreeTierUsage> _usageRepo;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "ActiveFreeTierConfig";

        public FreeTierService(
            IRepository<FreeTierConfig> configRepo,
            IRepository<FreeTierUsage> usageRepo,
            IMemoryCache cache)
        {
            _configRepo = configRepo;
            _usageRepo = usageRepo;
            _cache = cache;
        }

        private async Task<FreeTierConfig> ResolveActiveConfigAsync()
        {
            if (_cache.TryGetValue(CacheKey, out FreeTierConfig? cached) && cached != null)
                return cached;

            var configs = await _configRepo.GetAllAsync(c => c.IsActive);
            var active = configs.FirstOrDefault()
                ?? new FreeTierConfig { TokensPerWindow = 8000, WindowHours = 24 };

            _cache.Set(CacheKey, active, TimeSpan.FromMinutes(5));
            return active;
        }

        private async Task<FreeTierUsage> GetOrCreateUsageWithResetAsync(int userId)
        {
            var config = await ResolveActiveConfigAsync();
            var usages = await _usageRepo.GetAllAsync(u => u.UserId == userId);
            var usage = usages.FirstOrDefault();

            var now = DateTime.UtcNow;

            if (usage == null)
            {
                usage = new FreeTierUsage { UserId = userId, WindowStartAt = now, TokensUsedInWindow = 0 };
                await _usageRepo.AddAsync(usage);
                await _usageRepo.SaveChangesAsync();
                return usage;
            }

            if (now >= usage.WindowStartAt.AddHours(config.WindowHours))
            {
                usage.WindowStartAt = now;
                usage.TokensUsedInWindow = 0;
                _usageRepo.Update(usage);
                await _usageRepo.SaveChangesAsync();
            }

            return usage;
        }

        public async Task<int> GetRemainingFreeTokensAsync(int userId)
        {
            var config = await ResolveActiveConfigAsync();
            var usage = await GetOrCreateUsageWithResetAsync(userId);
            return Math.Max(0, config.TokensPerWindow - usage.TokensUsedInWindow);
        }

        public async Task DeductFreeTokensAsync(int userId, int tokensUsed)
        {
            var usage = await GetOrCreateUsageWithResetAsync(userId);
            usage.TokensUsedInWindow += tokensUsed;
            _usageRepo.Update(usage);
            await _usageRepo.SaveChangesAsync();
        }

        public async Task<IEnumerable<FreeTierConfigDto>> GetAllConfigsAsync()
        {
            var all = await _configRepo.GetAllWithIncludeAsync(null, c => c.UpdatedByUser);

            return all
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new FreeTierConfigDto
                {
                    Id = c.Id,
                    TokensPerWindow = c.TokensPerWindow,
                    WindowHours = c.WindowHours,
                    IsActive = c.IsActive,
                    UpdatedByUserName = c.UpdatedByUser?.FullName ?? "—",
                    CreatedAt = c.CreatedAt
                });
        }

        public async Task CreateConfigAsync(FreeTierConfigSaveDto dto, int userId)
        {
            if (dto.TokensPerWindow <= 0 || dto.TokensPerWindow > 200000)
                throw new InvalidOperationException("Số token phải trong khoảng 1 đến 200,000.");

            if (dto.WindowHours <= 0 || dto.WindowHours > 720)
                throw new InvalidOperationException("Thời gian cửa sổ phải trong khoảng 1 đến 720 giờ (30 ngày).");

            var existing = await _configRepo.GetAllAsync(c => c.IsActive);
            foreach (var old in existing)
            {
                old.IsActive = false;
                _configRepo.Update(old);
            }

            var newConfig = new FreeTierConfig
            {
                TokensPerWindow = dto.TokensPerWindow,
                WindowHours = dto.WindowHours,
                IsActive = true,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _configRepo.AddAsync(newConfig);
            await _configRepo.SaveChangesAsync();

            _cache.Remove(CacheKey);
        }
    }
}
