using SmartEdu.Business.Interfaces;

namespace SmartEdu.Web.Services
{
    /// <summary>
    /// Centralized, async helper around <see cref="IPermissionService"/> used by Razor
    /// views and Tag Helpers to make subject-scoped authorization decisions.
    /// Keeps the views thin and avoids duplicated permission logic.
    /// </summary>
    public class SubjectAccessAuthorizer
    {
        private readonly IPermissionService _permissionService;

        public SubjectAccessAuthorizer(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public Task<bool> CanAccessSubjectAsync(int userId, int subjectId)
            => _permissionService.CanUserAccessSubject(userId, subjectId);

        public Task<bool> IsLecturerLeaderAsync(int userId, int subjectId)
            => _permissionService.IsLecturerLeaderAsync(userId, subjectId);
    }
}