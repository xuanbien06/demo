using FaceAttendance.Web.Models;

namespace FaceAttendance.Web.Repositories
{
    public interface IFaceEmbeddingRepository
    {
        // Hàm lấy tất cả khuôn mặt kèm theo thông tin Sinh viên
        Task<List<FaceEmbedding>> GetAllWithStudentAsync();
    }
}