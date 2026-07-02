// Đường dẫn: FaceAttendance.Web/Controllers/AttendanceController.cs
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaceAttendance.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Recognize([FromBody] ImageRequest request)
        {
            // Trả về một List danh sách các người đang đứng trước Camera kèm tọa độ
            var facesResult = await _attendanceService.ProcessAttendanceAsync(request.Base64Image);

            // Gói vào JSON gửi xuống Javascript xử lý vẽ khung
            return Json(new { success = true, faces = facesResult });
        }
    }

    public class ImageRequest
    {
        public string Base64Image { get; set; } = string.Empty;
    }
}