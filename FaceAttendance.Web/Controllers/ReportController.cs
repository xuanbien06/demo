using FaceAttendance.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FaceAttendance.Web.Controllers
{
    // Cả Admin và Giảng viên đều được phép xem Báo cáo
    [Authorize(Roles = "Admin,Teacher")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Màn hình chính
        public async Task<IActionResult> Index()
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdString, out int userId);

            // LOGIC LỌC LỚP HỌC (Giống y hệt trang Điểm danh)
            // Admin thấy toàn bộ lớp, Giảng viên chỉ thấy lớp mình dạy
            var classes = userRole == "Admin"
                ? await _context.Classes.OrderByDescending(c => c.CreatedAt).ToListAsync()
                : await _context.Classes.Where(c => c.TeacherId == userId).OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.Classes = classes;
            return View();
        }

        // 2. API Tính toán dữ liệu báo cáo cho Lớp học
        [HttpGet]
        public async Task<IActionResult> GetClassReport(int classId)
        {
            // Bước 1: Lấy tổng số buổi đã điểm danh của Lớp này
            var sessionIds = await _context.AttendanceSessions
                .Where(s => s.ClassID == classId)
                .Select(s => s.SessionID)
                .ToListAsync();

            int totalSessions = sessionIds.Count;

            // Bước 2: Lấy danh sách Sinh viên đang học trong Lớp này
            var studentsInClass = await _context.ClassStudents
                .Include(cs => cs.Student)
                .Where(cs => cs.ClassID == classId)
                .Select(cs => new { cs.StudentID, cs.Student.FullName })
                .ToListAsync();

            // Bước 3: Đếm số buổi CÓ MẶT của từng Sinh viên trong các buổi của Lớp
            var attendanceData = await _context.AttendanceRecords
                .Where(r => sessionIds.Contains(r.SessionID) && r.IsPresent == true)
                .GroupBy(r => r.StudentID)
                .Select(g => new { StudentID = g.Key, PresentCount = g.Count() })
                .ToDictionaryAsync(g => g.StudentID, g => g.PresentCount);

            // Bước 4: Lắp ráp và tính phần trăm
            var finalData = studentsInClass.Select(s => {
                int presentCount = attendanceData.ContainsKey(s.StudentID) ? attendanceData[s.StudentID] : 0;
                int absentCount = totalSessions - presentCount;

                // Tránh lỗi chia cho 0 nếu lớp chưa học buổi nào
                double rate = totalSessions > 0 ? Math.Round((double)presentCount / totalSessions * 100, 2) : 0;

                return new
                {
                    StudentID = s.StudentID,
                    FullName = s.FullName,
                    TotalSessions = totalSessions,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    AttendanceRate = rate
                };
            }).OrderBy(s => s.StudentID); // Sắp xếp theo mã SV cho đẹp

            return Json(new { data = finalData });
        }
    }
}