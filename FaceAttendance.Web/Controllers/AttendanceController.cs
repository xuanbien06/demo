using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly AppDbContext _context;

        public AttendanceController(IAttendanceService attendanceService, AppDbContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        // 1. Giao diện mở Camera điểm danh
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var classes = await _context.Classes.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(classes);
        }

        // 2. API Nhận diện khuôn mặt liên tục
        [HttpPost]
        public async Task<IActionResult> Recognize([FromBody] ImageRequest request)
        {
            var facesResult = await _attendanceService.ProcessAttendanceAsync(request.Base64Image, request.ClassId);
            return Json(new { success = true, faces = facesResult });
        }

        // 3. API Lưu kết quả và Gửi Email khi nhấn Kết Thúc
        [HttpPost]
        public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
        {
            var session = new AttendanceSession
            {
                ClassID = request.ClassId,
                Date = DateTime.Now,
                IsCompleted = true
            };
            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            var classStudents = await _context.ClassStudents
                .Include(cs => cs.Student)
                .Include(cs => cs.ClassRoom)
                .Where(cs => cs.ClassID == request.ClassId)
                .ToListAsync();

            foreach (var cs in classStudents)
            {
                bool isPresent = request.PresentStudentIds.Contains(cs.StudentID);

                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    SessionID = session.SessionID,
                    StudentID = cs.StudentID,
                    IsPresent = isPresent,
                    Time = isPresent ? DateTime.Now : null
                });

                // Nếu vắng mặt thì Gửi Email Cảnh Báo
                if (!isPresent)
                {
                    try
                    {
                        var emailService = HttpContext.RequestServices.GetService(typeof(EmailService)) as EmailService;
                        if (emailService != null)
                        {
                            string subject = $"[CẢNH BÁO VẮNG HỌC] - Lớp {cs.ClassRoom.ClassName}";
                            string body = $"<h3>Kính gửi sinh viên: {cs.Student.FullName}</h3>" +
                                          $"<p>Hệ thống AI nhận thấy bạn đã <strong>VẮNG MẶT</strong> trong buổi học lớp {cs.ClassRoom.ClassName} ngày {DateTime.Now:dd/MM/yyyy}.</p>" +
                                          $"<p>Vui lòng liên hệ giảng viên nếu có sai sót.</p>";

                            _ = emailService.SendEmailAsync(cs.Student.Email, subject, body);
                        }
                    }
                    catch { /* Bỏ qua lỗi cấu hình Email để không sập web */ }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, sessionId = session.SessionID });
        }

        // 4. CHÍNH LÀ HÀM NÀY NAY ĐÃ ĐƯỢC ĐẶT ĐÚNG VỊ TRÍ
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.ClassRoom)
                .Include(s => s.Records)
                    .ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(s => s.SessionID == id);

            if (session == null) return NotFound();

            return View(session);
        }
    }

    // ==========================================================
    // CÁC CLASS PHỤ TRỢ (Bắt buộc phải nằm NGOÀI class Controller)
    // ==========================================================
    public class ImageRequest
    {
        public string Base64Image { get; set; } = string.Empty; 
        public int ClassId { get; set; }
    }

    public class EndSessionRequest
    {
        public int ClassId { get; set; }
        public List<string> PresentStudentIds { get; set; } = new List<string>();
    }
}