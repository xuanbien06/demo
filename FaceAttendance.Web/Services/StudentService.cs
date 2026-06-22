using FaceAttendance.Web.Models;
using FaceAttendance.Web.Repositories;

namespace FaceAttendance.Web.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepo;

        public StudentService(IStudentRepository studentRepo)
        {
            _studentRepo = studentRepo;
        }

        public async Task<IEnumerable<Student>> GetAllActiveStudentsAsync()
        {
            var allStudents = await _studentRepo.GetAllAsync();
            // Lọc bằng LINQ: Chỉ trả về những bạn có IsActive == true
            return allStudents.Where(s => s.IsActive == true);
        }
    }
}