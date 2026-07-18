using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }

        // BẢNG QUẢN LÝ HỌC VỤ
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Major> Majors { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        // BẢNG NGHIỆP VỤ CỐT LÕI
        public DbSet<Student> Students { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }
        public DbSet<ClassRoom> Classes { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.ClassID, cs.StudentID });

            modelBuilder.Entity<ClassRoom>()
                .HasIndex(c => c.ClassName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // RÀNG BUỘC KIẾN TRÚC: Ngăn lỗi vòng lặp Cascade Delete.
            // Nếu xóa Giảng viên khỏi hệ thống, Lớp học không bị xóa theo mà TeacherId sẽ thành NULL.
            modelBuilder.Entity<ClassRoom>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Quản trị viên toàn quyền hệ thống" },
                new Role { Id = 2, Name = "Teacher", Description = "Giảng viên quản lý lớp học" },
                new Role { Id = 3, Name = "Student", Description = "Sinh viên" }
            );
        }
    }
}