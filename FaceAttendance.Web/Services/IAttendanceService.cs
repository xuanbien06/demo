namespace FaceAttendance.Web.Services
{
    public interface IAttendanceService
    {
        // Hàm trả về một Tuple chứa 4 giá trị: Success, Message, Tên sinh viên, Phần trăm giống
        Task<(bool Success, string Message, string StudentName, double Percent)> ProcessAttendanceAsync(string base64Image);
    }
}