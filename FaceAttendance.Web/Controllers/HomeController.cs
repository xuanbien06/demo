using FaceAttendance.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    // BỔ SUNG ROLES: Chỉ Admin và Teacher mới được phép đi qua cánh cửa này
    [Authorize(Roles = "Admin,Teacher")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int totalStudents = await _context.Students.CountAsync();
            int activeStudents = await _context.Students.CountAsync(s => s.IsActive == true);
            int inactiveStudents = totalStudents - activeStudents;

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