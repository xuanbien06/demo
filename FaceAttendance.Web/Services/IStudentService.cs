using FaceAttendance.Web.Models;

namespace FaceAttendance.Web.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<Student>> GetAllActiveStudentsAsync();
    }
}