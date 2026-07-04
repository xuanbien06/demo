using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }

        // ĐÃ SỬA: Dùng ClassRoom
        public DbSet<ClassRoom> Classes { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.ClassID, cs.StudentID });

            // ĐÃ SỬA: Dùng ClassRoom
            modelBuilder.Entity<ClassRoom>()
                .HasIndex(c => c.ClassName)
                .IsUnique();
        }
    }
}