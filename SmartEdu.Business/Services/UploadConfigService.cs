using Microsoft.Extensions.Caching.Memory;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Business.Services
{
    public class UploadConfigService : IUploadConfigService
    {
        private readonly IRepository<UploadConfig> _repo;
        private readonly IMemoryCache _cache;
        private const int DefaultMaxFileSizeMB = 10;
        private const double DefaultNearDuplicateThreshold = 0.90;
        private const string ThresholdCacheKey = "ActiveNearDuplicateThreshold";
        private static readonly string[] AllowedFileTypes = { "pdf", "docx", "pptx" };

        public UploadConfigService(IRepository<UploadConfig> repo, IMemoryCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<IEnumerable<UploadConfigDto>> GetAllAsync()
        {
            var all = await _repo.GetAllWithIncludeAsync(
                null,
                c => c.Subject,
                c => c.UpdatedByUser);

            return all
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new UploadConfigDto
                {
                    Id = c.Id,
                    MaxFileSizeMB = c.MaxFileSizeMB,
                    FileType = c.FileType,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject?.Name,
                    IsActive = c.IsActive,
                    NearDuplicateThreshold = c.NearDuplicateThreshold,
                    UpdatedByUserName = c.UpdatedByUser?.FullName ?? "—",
                    CreatedAt = c.CreatedAt
                });
        }

        public async Task<int> ResolveMaxFileSizeMBAsync(int subjectId, string fileType)
        {
            fileType = fileType?.ToLowerInvariant().TrimStart('.') ?? string.Empty;
            var cacheKey = $"UploadConfig_{subjectId}_{fileType}";

            if (_cache.TryGetValue(cacheKey, out int cached))
                return cached;

            var configs = (await _repo.GetAllAsync(c => c.IsActive)).ToList();

            var result =
                configs.FirstOrDefault(c => c.SubjectId == subjectId && c.FileType == fileType)?.MaxFileSizeMB
                ?? configs.FirstOrDefault(c => c.SubjectId == subjectId && string.IsNullOrEmpty(c.FileType))?.MaxFileSizeMB
                ?? configs.FirstOrDefault(c => c.SubjectId == null && c.FileType == fileType)?.MaxFileSizeMB
                ?? configs.FirstOrDefault(c => c.SubjectId == null && string.IsNullOrEmpty(c.FileType))?.MaxFileSizeMB
                ?? DefaultMaxFileSizeMB;

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        public async Task<double> ResolveNearDuplicateThresholdAsync()
        {
            if (_cache.TryGetValue(ThresholdCacheKey, out double cached))
                return cached;

            // Chỉ lấy đúng bản ghi Global (SubjectId = null, FileType = null) đang active
            var globalConfig = await _repo.GetAllAsync(c =>
                c.IsActive && c.SubjectId == null && string.IsNullOrEmpty(c.FileType) && c.NearDuplicateThreshold != null);

            var result = globalConfig.FirstOrDefault()?.NearDuplicateThreshold ?? DefaultNearDuplicateThreshold;

            _cache.Set(ThresholdCacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        public async Task CreateAsync(UploadConfigSaveDto dto, int userId)
        {
            if (dto.MaxFileSizeMB <= 0 || dto.MaxFileSizeMB > 500)
                throw new InvalidOperationException("Kích thước file phải trong khoảng 1MB đến 500MB.");

            var normalizedFileType = string.IsNullOrWhiteSpace(dto.FileType)
                ? null
                : dto.FileType.Trim().ToLowerInvariant();

            if (normalizedFileType != null && !AllowedFileTypes.Contains(normalizedFileType))
                throw new InvalidOperationException("Loại file không hợp lệ. Chỉ hỗ trợ pdf, docx, pptx.");

            bool isGlobalScope = dto.SubjectId == null && normalizedFileType == null;

            // Threshold chỉ được phép set khi tạo config phạm vi Global
            double? thresholdToSave = null;
            if (isGlobalScope && dto.NearDuplicateThreshold.HasValue)
            {
                if (dto.NearDuplicateThreshold.Value < 0.5 || dto.NearDuplicateThreshold.Value > 1.0)
                    throw new InvalidOperationException("Ngưỡng phát hiện gần giống phải trong khoảng 0.5 đến 1.0.");

                thresholdToSave = dto.NearDuplicateThreshold.Value;
            }

            var existing = await _repo.GetAllAsync(c =>
                c.IsActive && c.SubjectId == dto.SubjectId && c.FileType == normalizedFileType);

            foreach (var old in existing)
            {
                old.IsActive = false;
                _repo.Update(old);
            }

            var newConfig = new UploadConfig
            {
                MaxFileSizeMB = dto.MaxFileSizeMB,
                FileType = normalizedFileType,
                SubjectId = dto.SubjectId,
                NearDuplicateThreshold = thresholdToSave,
                IsActive = true,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(newConfig);
            await _repo.SaveChangesAsync();

            _cache.Remove($"UploadConfig_{dto.SubjectId}_{normalizedFileType}");
            if (isGlobalScope) _cache.Remove(ThresholdCacheKey);
        }
    }
}