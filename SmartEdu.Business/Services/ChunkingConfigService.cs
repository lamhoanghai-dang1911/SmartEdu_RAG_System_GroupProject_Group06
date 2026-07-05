using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Services
{
    public class ChunkingConfigService : IChunkingConfigService
    {
        private readonly IRepository<ChunkingConfig> _configRepo;
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IRepository<User> _userRepo;

        public ChunkingConfigService(
            IRepository<ChunkingConfig> configRepo,
            IRepository<Subject> subjectRepo,
            IRepository<User> userRepo)
        {
            _configRepo = configRepo;
            _subjectRepo = subjectRepo;
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<ChunkingConfigDto>> GetAllAsync()
        {
            var configs = await _configRepo.GetAllWithIncludeAsync(
                c => !c.IsDeleted,
                c => c.Subject
            );

            var userIds = configs.Select(c => c.UpdatedByUserId).Distinct().ToList();
            var users = await _userRepo.GetAllAsync(u => userIds.Contains(u.Id));
            var userMap = users.ToDictionary(u => u.Id, u => u.FullName);

            return configs
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new ChunkingConfigDto
                {
                    Id = c.Id,
                    ChunkSize = c.ChunkSize,
                    ChunkOverlap = c.ChunkOverlap,
                    Strategy = c.Strategy,
                    Scope = c.Scope,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject?.Name,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedByUserName = userMap.GetValueOrDefault(c.UpdatedByUserId, "N/A")
                });
        }

        public async Task<ChunkingConfigDto?> GetByIdAsync(int id)
        {
            var c = await _configRepo.GetByIdAsync(id);
            if (c == null || c.IsDeleted) return null;

            return new ChunkingConfigDto
            {
                Id = c.Id,
                ChunkSize = c.ChunkSize,
                ChunkOverlap = c.ChunkOverlap,
                Strategy = c.Strategy,
                Scope = c.Scope,
                SubjectId = c.SubjectId,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            };
        }

        public async Task SaveAsync(ChunkingConfigSaveDto dto, int updatedByUserId)
        {
            // ==== Validate business rule ====
            if (dto.ChunkSize < 200 || dto.ChunkSize > 2000)
                throw new InvalidOperationException("Chunk size phải trong khoảng 200–2000.");

            if (dto.ChunkOverlap < 0 || dto.ChunkOverlap >= dto.ChunkSize / 2)
                throw new InvalidOperationException("Chunk overlap phải nhỏ hơn một nửa Chunk size.");

            if (dto.Scope == ChunkingScope.Global && dto.SubjectId.HasValue)
                throw new InvalidOperationException("Config phạm vi Global không được gắn Subject.");

            if (dto.Scope == ChunkingScope.PerSubject)
            {
                if (!dto.SubjectId.HasValue)
                    throw new InvalidOperationException("Config phạm vi PerSubject bắt buộc chọn Subject.");

                var subject = await _subjectRepo.GetByIdAsync(dto.SubjectId.Value);
                if (subject == null)
                    throw new InvalidOperationException("Subject không tồn tại.");
            }

            // ==== Deactivate config active cũ cùng scope/subject (giữ lịch sử, không xoá) ====
            var existingActive = await _configRepo.GetAllAsync(c =>
                c.IsActive && !c.IsDeleted && c.Scope == dto.Scope &&
                (dto.Scope == ChunkingScope.Global || c.SubjectId == dto.SubjectId));

            foreach (var old in existingActive)
            {
                old.IsActive = false;
                old.UpdatedAt = DateTime.UtcNow;
                _configRepo.Update(old);
            }

            // ==== Tạo config mới, đánh dấu Active ====
            var newConfig = new ChunkingConfig
            {
                ChunkSize = dto.ChunkSize,
                ChunkOverlap = dto.ChunkOverlap,
                Strategy = dto.Strategy,
                Scope = dto.Scope,
                SubjectId = dto.Scope == ChunkingScope.PerSubject ? dto.SubjectId : null,
                IsActive = true,
                UpdatedByUserId = updatedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _configRepo.AddAsync(newConfig);
            await _configRepo.SaveChangesAsync();
        }

        public async Task<ChunkingConfig> ResolveActiveConfigAsync(int subjectId)
        {
            var perSubject = await _configRepo.GetAllAsync(c =>
                c.Scope == ChunkingScope.PerSubject && c.SubjectId == subjectId && c.IsActive && !c.IsDeleted);
            var config = perSubject.FirstOrDefault();
            if (config != null) return config;

            var global = await _configRepo.GetAllAsync(c =>
                c.Scope == ChunkingScope.Global && c.IsActive && !c.IsDeleted);
            config = global.FirstOrDefault();
            if (config != null) return config;

            throw new InvalidOperationException("Chưa có ChunkingConfig nào được cấu hình. Vui lòng liên hệ Admin.");
        }
    }
}
