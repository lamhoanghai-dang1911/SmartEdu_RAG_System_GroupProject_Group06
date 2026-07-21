using Microsoft.EntityFrameworkCore;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using SmartEdu.Shared.Helpers;

namespace SmartEdu.Business.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IRepository<Subject> _repo;
        private readonly IRepository<Document> _documentRepo;
        private readonly IRepository<StudentSubject> _studentSubjectRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;
        private readonly IRealtimeNotifier _realtime;


        public SubjectService(
    IRepository<Subject> repo,
    IRepository<Document> documentRepo,
    IRepository<StudentSubject> studentSubjectRepo,
    IRepository<User> userRepo,
    IUnitOfWork uow,
    IEmailService emailService,
    IRealtimeNotifier realtime)
        {
            _repo = repo;
            _documentRepo = documentRepo;
            _studentSubjectRepo = studentSubjectRepo;
            _userRepo = userRepo;
            _uow = uow;
            _emailService = emailService;
            _realtime = realtime;
        }

        // Assign giảng viên cho subject
        public async Task AssignLecturerToSubject(AssignLecturerDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var subject = await _uow.Subjects.GetByIdAsync(dto.SubjectId);
            if (subject == null || subject.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy môn học.");

            await _uow.BeginTransactionAsync();
            try
            {
                var existing = await _uow.LecturerSubjects.GetAllAsync();
                var item = existing.FirstOrDefault(ls => ls.LecturerId == dto.LecturerId && ls.SubjectId == dto.SubjectId);

                if (item == null)
                {
                    await _uow.LecturerSubjects.AddAsync(new LecturerSubject
                    {
                        LecturerId = dto.LecturerId,
                        SubjectId = dto.SubjectId,
                        IsLeader = dto.IsLeader
                    });
                }
                else
                {
                    item.IsLeader = dto.IsLeader;
                    _uow.LecturerSubjects.Update(item);
                }

                if (dto.IsLeader)
                {
                    var leaders = existing.Where(ls => ls.SubjectId == dto.SubjectId && ls.IsLeader && ls.LecturerId != dto.LecturerId).ToList();
                    foreach (var l in leaders)
                    {
                        l.IsLeader = false;
                        _uow.LecturerSubjects.Update(l);
                    }
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }

            // === Bắn realtime cho giảng viên vừa được assign ===
            try
            {
                var subjectDto = new SubjectDto
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    Description = subject.Description,
                    CreatedAt = subject.CreatedAt
                };
                await _realtime.SendSubjectAssignedToLecturerAsync(dto.LecturerId, subjectDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast subject assigned: {ex}");
            }
        }

        // Kiểm tra leader được phép upload tài liệu
        public async Task<bool> CanUploadDocument(int lecturerId, int subjectId)
        {
            var rels = await _uow.LecturerSubjects.GetAllAsync();
            var item = rels.FirstOrDefault(ls => ls.LecturerId == lecturerId && ls.SubjectId == subjectId && ls.IsLeader);
            return item != null;
        }

        public async Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId)
        {
            var rels = await _uow.LecturerSubjects.GetAllWithIncludeAsync(ls => ls.LecturerId == lecturerId, ls => ls.Subject);
            var subjects = rels.Where(r => r.Subject != null && !r.Subject.IsDeleted)
                                .Select(r => r.Subject)
                                .Distinct()
                                .Select(s => new SubjectDto
                                {
                                    Id = s.Id,
                                    Name = s.Name,
                                    Description = s.Description,
                                    CreatedAt = s.CreatedAt
                                });

            return subjects;
        }

        public async Task RemoveLecturerFromSubject(int lecturerId, int subjectId)
        {
            var rels = await _uow.LecturerSubjects.GetAllAsync();
            var item = rels.FirstOrDefault(ls => ls.LecturerId == lecturerId && ls.SubjectId == subjectId);
            if (item == null) return;

            _uow.LecturerSubjects.Delete(item);
            await _uow.SaveChangesAsync();

            try
            {
                await _realtime.SendSubjectUnassignedFromLecturerAsync(lecturerId, subjectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast subject unassigned: {ex}");
            }
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var all = await _repo.GetAllAsync();
            return all.Where(s => !s.IsDeleted).Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedAt = s.CreatedAt
            });
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var subject = await _repo.GetByIdAsync(id);
            if (subject == null || subject.IsDeleted) return null;

            return new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                CreatedAt = subject.CreatedAt
            };
        }

        public async Task<SubjectDto> CreateAsync(SubjectCreateDto dto)
        {
            var subject = new Subject
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddAsync(subject);
            await _repo.SaveChangesAsync();

            var result = new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                CreatedAt = subject.CreatedAt
            };

            try
            {
                await _realtime.SendSubjectCreatedAsync(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast subject created: {ex}");
            }

            return result;
        }

        public async Task UpdateAsync(SubjectUpdateDto dto)
        {
            var existingSubject = await _repo.GetByIdAsync(dto.Id);
            if (existingSubject == null || existingSubject.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy môn học");

            existingSubject.Name = dto.Name;
            existingSubject.Description = dto.Description;
            existingSubject.UpdatedAt = DateTime.UtcNow;

            _repo.Update(existingSubject);
            await _repo.SaveChangesAsync();

            var subjectDto = new SubjectDto
            {
                Id = existingSubject.Id,
                Name = existingSubject.Name,
                Description = existingSubject.Description,
                CreatedAt = existingSubject.CreatedAt
            };

            try
            {
                var lecturerRels = await _uow.LecturerSubjects.GetAllAsync(ls => ls.SubjectId == dto.Id);
                var studentRels = await _studentSubjectRepo.GetAllAsync(ss => ss.SubjectId == dto.Id && !ss.IsDeleted);

                var affectedUserIds = lecturerRels.Select(ls => ls.LecturerId)
                    .Concat(studentRels.Select(ss => ss.StudentId))
                    .Distinct();

                await _realtime.SendSubjectUpdatedAsync(subjectDto, affectedUserIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast subject updated: {ex}");
            }
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _repo.GetByIdAsync(id);
            if (subject is null) return;

            var lecturerRels = (await _uow.LecturerSubjects
                .GetAllAsync(ls => ls.SubjectId == id)).ToList();
            var studentRels = (await _studentSubjectRepo
                .GetAllAsync(ss => ss.SubjectId == id && !ss.IsDeleted)).ToList();
            var documents = (await _documentRepo
                .GetAllAsync(d => d.SubjectId == id && !d.IsDeleted)).ToList();

            var affectedUserIds = lecturerRels.Select(ls => ls.LecturerId)
                .Concat(studentRels.Select(ss => ss.StudentId))
                .Distinct()
                .ToList();

            var deletedAt = DateTime.UtcNow;
            await _uow.BeginTransactionAsync();

            try
            {
                subject.IsDeleted = true;
                subject.UpdatedAt = deletedAt;
                _repo.Update(subject);

                foreach (var document in documents)
                {
                    document.IsDeleted = true;
                    document.UpdatedAt = deletedAt;
                    _documentRepo.Update(document);
                }

                // Dọn luôn các bản ghi phân công giảng viên trỏ tới môn đã xóa
                foreach (var rel in lecturerRels)
                {
                    _uow.LecturerSubjects.Delete(rel);
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }

            foreach (var document in documents)
            {
                try
                {
                    await _realtime.SendDocumentDeletedAsync(document.Id, id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to broadcast document {document.Id} deleted: {ex}");
                }
            }

            try
            {
                await _realtime.SendSubjectDeletedAsync(id, affectedUserIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast subject deleted: {ex}");
            }
        }

        public async Task<IEnumerable<SubjectDto>> GetSubjectsByUserIdAsync(int userId)
        {
            var enrollments = await _studentSubjectRepo.GetAllWithIncludeAsync(
                ss => ss.StudentId == userId && !ss.IsDeleted,
                ss => ss.Subject);

            return enrollments.Select(ss => ss.Subject)
                              .Where(s => s != null && !s.IsDeleted)
                              .Distinct()
                              .Select(s => new SubjectDto
                              {
                                  Id = s.Id,
                                  Name = s.Name,
                                  Description = s.Description,
                                  CreatedAt = s.CreatedAt,
                              });
        }

        // Assign sinh viên vào môn học
        public async Task AssignStudentToSubject(int studentId, int subjectId)
        {
            var existing = await _studentSubjectRepo.GetAllAsync();
            var item = existing.FirstOrDefault(ss => ss.StudentId == studentId && ss.SubjectId == subjectId);

            if (item == null)
            {
                await _studentSubjectRepo.AddAsync(new StudentSubject { StudentId = studentId, SubjectId = subjectId });
            }
            else if (item.IsDeleted)
            {
                item.IsDeleted = false;
                _studentSubjectRepo.Update(item);
            }
            await _studentSubjectRepo.SaveChangesAsync();
        }

        public async Task RemoveStudentFromSubject(int studentId, int subjectId)
        {
            var enrollments = await _studentSubjectRepo.GetAllAsync();
            var item = enrollments.FirstOrDefault(ss => ss.StudentId == studentId && ss.SubjectId == subjectId && !ss.IsDeleted);

            if (item != null)
            {
                item.IsDeleted = true;
                _studentSubjectRepo.Update(item);
                await _studentSubjectRepo.SaveChangesAsync();
            }
        }

        // Render 2 state for admin 
        public async Task<(IEnumerable<UserDto> Enrolled, IEnumerable<UserDto> NotEnrolled)> GetStudentEnrollmentStatus(int subjectId)
        {
            var allStudents = await _userRepo.GetAllAsync(u => u.Role == UserRole.Student && !u.IsDeleted);

            var enrollments = await _studentSubjectRepo.GetAllWithIncludeAsync(
                ss => ss.SubjectId == subjectId && !ss.IsDeleted,
                ss => ss.User
            );

            var enrolledIds = enrollments.Select(e => e.StudentId).ToList();

            var enrolledDtos = enrollments.Select(e => new UserDto
            {
                Id = e.User.Id,
                Username = e.User.Username,
                FullName = e.User.FullName,
                Role = e.User.Role,
                Email = e.User.Email,
                StudentCode = e.User.StudentCode
            });

            var notEnrolledDtos = allStudents
                .Where(u => !enrolledIds.Contains(u.Id))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    Email = u.Email,
                    StudentCode = u.StudentCode
                });

            return (Enrolled: enrolledDtos, NotEnrolled: notEnrolledDtos);
        }

        // Import danh sách sinh viên từ excel
        public async Task ImportStudentsAsync(int subjectId, List<StudentImportDto> importedStudents)
        {
            if (importedStudents == null || !importedStudents.Any()) return;

            var subject = await _uow.Subjects.GetByIdAsync(subjectId);
            if (subject == null || subject.IsDeleted) // Check subject
                throw new InvalidOperationException("Không tìm thấy môn học.");

            await _uow.BeginTransactionAsync();

            try
            {
                var importedEmails = importedStudents.Select(s => s.Email.Trim().ToLower()).ToList();
                var importedCodes = importedStudents.Select(s => s.StudentCode.Trim().ToUpper()).ToList();

                var existingUsers = await _uow.Users.GetAllAsync(u =>
                    !u.IsDeleted && (importedEmails.Contains(u.Email.ToLower()) || importedCodes.Contains(u.StudentCode.ToUpper()))
                );

                var existingEmails = existingUsers.Select(u => u.Email.ToLower()).ToHashSet();
                var existingCodes = existingUsers.Select(u => u.StudentCode?.ToUpper()).ToHashSet();

                var newUsersToInsert = new List<User>();
                var generatedAccountsLog = new List<(string Email, string FullName, string Username, string PlainPassword)>();

                foreach (var student in importedStudents)
                {
                    if (!existingEmails.Contains(student.Email.Trim().ToLower()) &&
                        !existingCodes.Contains(student.StudentCode.Trim().ToUpper()))
                    {
                        string username = ImportHelper.GenerateUsername(student.FullName, student.StudentCode);
                        string plainPassword = ImportHelper.GenerateRandomPassword(15);

                        newUsersToInsert.Add(new User
                        {
                            Username = username,
                            FullName = student.FullName.Trim(),
                            Email = student.Email.Trim().ToLower(),
                            StudentCode = student.StudentCode.Trim().ToUpper(),
                            Role = UserRole.Student,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                            RequirePasswordChange = true,
                        });

                        generatedAccountsLog.Add((student.Email, student.FullName, username, plainPassword));
                    }
                }

                if (newUsersToInsert.Any())
                {
                    foreach (var user in newUsersToInsert) await _uow.Users.AddAsync(user);
                    await _uow.SaveChangesAsync();
                }

                var allStudentIds = existingUsers.Select(u => u.Id)
                                             .Concat(newUsersToInsert.Select(u => u.Id))
                                             .ToList();

                var currentEnrollments = await _uow.StudentSubjects.GetAllAsync(ss => ss.SubjectId == subjectId);
                var enrolledStudentIds = currentEnrollments.Select(ss => ss.StudentId).ToHashSet();

                var newlyEnrolledIds = new List<int>();
                foreach (var studentId in allStudentIds)
                {
                    if (!enrolledStudentIds.Contains(studentId))
                    {
                        await _uow.StudentSubjects.AddAsync(new StudentSubject { StudentId = studentId, SubjectId = subjectId });
                        newlyEnrolledIds.Add(studentId);
                    }
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                // Send welcome emails for newly created accounts
                foreach (var account in generatedAccountsLog)
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(
                            account.Email, account.FullName, account.Username, account.PlainPassword);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi gửi mail cho {account.Email}: {ex.Message}");
                    }
                }

                // Send enrollment notification to all students who were newly enrolled into this subject
                if (newlyEnrolledIds.Any())
                {
                    var usersToNotify = await _uow.Users.GetAllAsync(u => newlyEnrolledIds.Contains(u.Id));
                    foreach (var u in usersToNotify)
                    {
                        try
                        {
                            await _emailService.SendEnrollmentNotificationAsync(u.Email, u.FullName, subject.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi gửi mail thông báo nhập học cho {u.Email}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception)
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> IsLecturerAssignedToSubject(int lecturerId, int subjectId)
        {
            var rels = await _uow.LecturerSubjects.GetAllAsync(
                ls => ls.LecturerId == lecturerId && ls.SubjectId == subjectId);
            return rels.Any();
        }

        public async Task<HashSet<int>> GetLeaderSubjectIdsAsync(int lecturerId)
        {
            var rels = await _uow.LecturerSubjects.GetAllAsync(
                ls => ls.LecturerId == lecturerId && ls.IsLeader);

            return rels.Select(ls => ls.SubjectId).ToHashSet();
        }
    }
}
