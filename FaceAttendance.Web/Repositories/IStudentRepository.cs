using FaceAttendance.Web.Models;

namespace FaceAttendance.Web.Repositories
{
    // Khai báo các hành động có thể làm với bảng Student
    public interface IStudentRepository
    {
        // Dùng Task để chạy bất đồng bộ (async), giúp web không bị đơ khi tải dữ liệu lớn
        Task<IEnumerable<Student>> GetAllAsync();
        Task AddAsync(Student student);
    }
}