using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace SmartEdu.Data
{
    public static class DataSeeder
    {
        public static void Seed(AppDbContext context)
        {
            SeedUsers(context);
            SeedChunkingConfig(context);
            SeedPackages(context);
        }

        private static void SeedUsers(AppDbContext context)
        {
            // Danh sách các user muốn đảm bảo luôn có trong hệ thống
            var users = new List<User>
    {
        new User { Username = "admin", FullName = "Quản trị viên", Role = UserRole.Admin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") },
        new User { Username = "lecturer", FullName = "Giảng viên mẫu", Role = UserRole.Lecturer, PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") },
        new User { Username = "lecturer2", FullName = "Giảng viên mẫu 2", Role = UserRole.Lecturer, PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") },
        new User { Username = "student", FullName = "Sinh viên mẫu 1", Role = UserRole.Student, PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") },
        new User { Username = "student2", FullName = "Sinh viên mẫu 2", Role = UserRole.Student, PasswordHash = BCrypt.Net.BCrypt.HashPassword("123") }
    };

            bool hasChanges = false;

            foreach (var user in users)
            {
                // Kiểm tra xem user đã tồn tại trong DB chưa dựa trên Username
                if (!context.Users.Any(u => u.Username == user.Username))
                {
                    context.Users.Add(user);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                context.SaveChanges();
            }
        }

        private static void SeedChunkingConfig(AppDbContext context)
        {
            if (context.ChunkingConfigs.Any()) return;

            var admin = context.Users.FirstOrDefault(u => u.Role == UserRole.Admin);
            if (admin == null) return; // cần Admin đã seed trước đó để gán UpdatedByUserId

            var config = new ChunkingConfig
            {
                ChunkSize = 800,
                ChunkOverlap = 80,
                Strategy = ChunkingStrategy.FixedSize,
                Scope = ChunkingScope.Global,
                SubjectId = null,
                IsActive = true,
                UpdatedByUserId = admin.Id
            };

            context.ChunkingConfigs.Add(config);
            context.SaveChanges();
        }

        private static void SeedPackages(AppDbContext context)
        {
            if (context.Packages.Any()) return;

            var packages = new List<Package>
            {
                new Package
                {
                    Name = "Gói Cơ Bản",
                    Description = "Dành cho sinh viên sử dụng cơ bản, giới hạn token thấp.",
                    Price = 49000,
                    DurationDays = 30,
                    TokenQuota = 50000,
                    IsActive = true
                },
                new Package
                {
                    Name = "Gói Nâng Cao",
                    Description = "Tăng quota token, phù hợp ôn thi cao điểm.",
                    Price = 99000,
                    DurationDays = 30,
                    TokenQuota = 150000,
                    IsActive = true
                },
                new Package
                {
                    Name = "Gói Học Kỳ",
                    Description = "Sử dụng thoải mái trong suốt học kỳ.",
                    Price = 299000,
                    DurationDays = 120,
                    TokenQuota = 600000,
                    IsActive = true
                }
            };

            context.Packages.AddRange(packages);
            context.SaveChanges();
        }
    }
}