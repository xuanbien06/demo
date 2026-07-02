// Đường dẫn: FaceAttendance.Web/Services/IAttendanceService.cs
namespace FaceAttendance.Web.Services
{
    // Tạo Class chứa kết quả trả về cho giao diện (Có Bounding Box)
    public class FaceResultResponse
    {
        public int[] Box { get; set; } = new int[4];
        public string StudentName { get; set; } = string.Empty;
        public double Percent { get; set; }
        public bool Success { get; set; }
    }

    public interface IAttendanceService
    {
        // Trả về một List (Nhiều khuôn mặt) thay vì Tuple (1 khuôn mặt)
        Task<List<FaceResultResponse>> ProcessAttendanceAsync(string base64Image);
    }
}