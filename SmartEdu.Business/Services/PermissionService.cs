using Microsoft.EntityFrameworkCore;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IRepository<StudentSubject> _studentSubjectRepo;
        private readonly IRepository<User> _userRepo;

        public PermissionService(
            IRepository<StudentSubject> studentSubjectRepo,
            IRepository<User> userRepo)
        {
            _studentSubjectRepo = studentSubjectRepo;
            _userRepo = userRepo;
        }

        public async Task<bool> CanUserAccessSubject(int userId, int subjectId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Lecturer))
            {
                return true;
            }
            var enrollments = await _studentSubjectRepo.GetAllAsync(
                ss => ss.StudentId == userId && ss.SubjectId == subjectId && !ss.IsDeleted
            );

            return enrollments.Any();
        }
    }
}
