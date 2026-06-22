using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        // Tiêm (Inject) AppDbContext vào để dùng
        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            // Lấy toàn bộ sinh viên từ Database trả về dạng List
            return await _context.Students.ToListAsync();
        }

        public async Task AddAsync(Student student)
        {
            // Thêm sinh viên mới vào bộ nhớ đệm của EF Core
            await _context.Students.AddAsync(student);
            // Lưu thay đổi xuống SQL Server thật
            await _context.SaveChangesAsync();
        }
    }
}