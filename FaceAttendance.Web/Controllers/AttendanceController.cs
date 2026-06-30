using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaceAttendance.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        // Tiêm (Inject) Service vào Controller
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
            // Controller bây giờ chỉ làm 1 việc duy nhất: Gọi Service và Trả kết quả
            var result = await _attendanceService.ProcessAttendanceAsync(request.Base64Image);

            if (result.Success)
            {
                return Json(new { success = true, studentName = result.StudentName, percent = result.Percent });
            }

            return Json(new { success = false, message = result.Message });
        }
    }

    public class ImageRequest
    {
        public string Base64Image { get; set; }
    }
}