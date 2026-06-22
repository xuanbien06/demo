using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Bảng Sinh viên (Em đã có sẵn từ phần trước)
        public DbSet<Student> Students { get; set; }

        // BẮT BUỘC THÊM DÒNG NÀY ĐỂ FIX LỖI:
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }
    }
}