using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }

        // --- CODE MỚI THÊM VÀO BÊN DƯỚI ---
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình khóa chính kép cho bảng trung gian ClassStudent
            modelBuilder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.ClassID, cs.StudentID });

            // 2. Ràng buộc Tên lớp không được trùng lặp (Validate cấp Database)
            modelBuilder.Entity<Class>()
                .HasIndex(c => c.ClassName)
                .IsUnique();
        }
    }
}