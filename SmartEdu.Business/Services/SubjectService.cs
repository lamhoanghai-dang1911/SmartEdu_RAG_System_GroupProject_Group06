using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Business.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IRepository<Subject> _repo;

        public SubjectService(IRepository<Subject> repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Subject>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<Subject?> GetByIdAsync(int id)
            => await _repo.GetByIdAsync(id);

        public async Task CreateAsync(Subject subject)
        {
            subject.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(subject);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subject subject)
        {
            subject.UpdatedAt = DateTime.UtcNow;
            _repo.Update(subject);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _repo.GetByIdAsync(id);
            if (subject is null) return;
            subject.IsDeleted = true;
            subject.UpdatedAt = DateTime.UtcNow;
            _repo.Update(subject);
            await _repo.SaveChangesAsync();
        }
    }
}
