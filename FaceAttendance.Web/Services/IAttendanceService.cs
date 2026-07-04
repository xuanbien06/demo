using System.Collections.Generic;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Services
{
    // Class chứa kết quả trả về cho giao diện (Có Bounding Box)
    public class FaceResultResponse
    {
        public int[] Box { get; set; } = new int[4];
        public string StudentId { get; set; } = string.Empty; // MỚI THÊM DÒNG NÀY
        public string StudentName { get; set; } = string.Empty;
        public double Percent { get; set; }
        public bool Success { get; set; }
    }

    public interface IAttendanceService
    {
        // ĐÃ THÊM: tham số int classId để biết đang quét cho lớp nào
        Task<List<FaceResultResponse>> ProcessAttendanceAsync(string base64Image, int classId);
    }
}