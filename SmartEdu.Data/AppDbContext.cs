using Microsoft.EntityFrameworkCore;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LecturerSubject> LecturerSubjects { get; set; }
        public DbSet<DocumentLog> DocumentLogs { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<ChunkingConfig> ChunkingConfigs { get; set; }
        public DbSet<EmbeddingSet> EmbeddingSets { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UsageLog> UsageLogs { get; set; }
        public DbSet<ModelComparisonLog> ModelComparisonLogs { get; set; }
        public DbSet<UploadConfig> UploadConfigs { get; set; }
        public DbSet<FreeTierConfig> FreeTierConfigs { get; set; }
        public DbSet<FreeTierUsage> FreeTierUsages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== ChunkingConfig =====
            modelBuilder.Entity<ChunkingConfig>(entity =>
            {
                entity.HasOne(c => c.Subject)
                      .WithMany()
                      .HasForeignKey(c => c.SubjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(c => c.UpdatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== EmbeddingSet =====
            modelBuilder.Entity<EmbeddingSet>(entity =>
            {
                entity.HasIndex(e => new { e.FileHash, e.ChunkingConfigId })
                      .IsUnique(); // key để tái sử dụng embedding

                entity.HasOne(e => e.ChunkingConfig)
                      .WithMany()
                      .HasForeignKey(e => e.ChunkingConfigId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SourceDocument)
                      .WithMany()
                      .HasForeignKey(e => e.SourceDocumentId)
                      .OnDelete(DeleteBehavior.Restrict); // tránh cascade vòng với Document
            });

            // ===== Document =====
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasOne(d => d.Subject)
                      .WithMany(s => s.Documents)
                      .HasForeignKey(d => d.SubjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ParentDocument)
                      .WithMany()
                      .HasForeignKey(d => d.ParentDocumentId)
                      .OnDelete(DeleteBehavior.Restrict); // self-reference, bắt buộc Restrict

                entity.HasOne(d => d.EmbeddingSet)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(d => d.EmbeddingSetId)
                      .OnDelete(DeleteBehavior.Restrict); // tránh cascade vòng với EmbeddingSet

                entity.HasIndex(d => d.FileHash); // không unique, vì trùng hash là hợp lệ (versioning + reuse)
            });

            // ===== DocumentChunk =====
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.HasOne(c => c.EmbeddingSet)
                      .WithMany(e => e.Chunks)
                      .HasForeignKey(c => c.EmbeddingSetId)
                      .OnDelete(DeleteBehavior.Cascade); // xoá EmbeddingSet thì xoá luôn chunk của nó
            });

            // ===== Package / Order / UserSubscription =====
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(o => o.TransactionCode).IsUnique();

                entity.HasOne(o => o.User)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Package)
                      .WithMany()
                      .HasForeignKey(o => o.PackageId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(o => o.Amount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasOne(s => s.User)
                      .WithMany()
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Package)
                      .WithMany()
                      .HasForeignKey(s => s.PackageId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Order)
                      .WithMany()
                      .HasForeignKey(s => s.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            });

            // ===== UsageLog =====
            modelBuilder.Entity<UsageLog>(entity =>
            {
                entity.HasOne(u => u.User)
                      .WithMany()
                      .HasForeignKey(u => u.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(u => new { u.UserId, u.CreatedAt }); // tăng tốc query thống kê theo ngày/tuần/tháng
            });
            // ===== LecturerSubject (composite key) =====
            modelBuilder.Entity<LecturerSubject>(entity =>
            {
                entity.HasKey(ls => new { ls.LecturerId, ls.SubjectId });

                entity.HasOne(ls => ls.Lecturer)
                      .WithMany()
                      .HasForeignKey(ls => ls.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ls => ls.Subject)
                      .WithMany()
                      .HasForeignKey(ls => ls.SubjectId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== StudentSubject (composite key) =====
            modelBuilder.Entity<StudentSubject>(entity =>
            {
                entity.HasKey(ss => new { ss.StudentId, ss.SubjectId });

                entity.HasOne(ss => ss.User)
                      .WithMany()
                      .HasForeignKey(ss => ss.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ss => ss.Subject)
                      .WithMany()
                      .HasForeignKey(ss => ss.SubjectId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== ModelComparisonLog =====
            // Không có FK đặc biệt, giữ mặc định
        }
    }
}
