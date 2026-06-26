// Đường dẫn: FaceAttendance.Web/Controllers/HomeController.cs
using FaceAttendance.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        // Tiêm (Inject) Database vào để lấy dữ liệu
        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Đếm tổng số sinh viên có trong Database
            int totalStudents = await _context.Students.CountAsync();

            // 2. Đếm số sinh viên Đang hoạt động (IsActive == true)
            int activeStudents = await _context.Students.CountAsync(s => s.IsActive == true);

            // 3. Tính số sinh viên Bảo lưu/Đã nghỉ
            int inactiveStudents = totalStudents - activeStudents;

            // 4. Dùng túi chứa ViewBag để quăng dữ liệu từ C# sang View (HTML)
            ViewBag.TotalStudents = totalStudents;
            ViewBag.ActiveStudents = activeStudents;
            ViewBag.InactiveStudents = inactiveStudents;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}