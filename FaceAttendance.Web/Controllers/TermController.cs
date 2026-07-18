using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TermController : Controller
    {
        private readonly AppDbContext _context;

        public TermController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /Term/Index -> Trả về giao diện Gộp (Tabs)
        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // QUẢN LÝ KHÓA HỌC (ACADEMIC YEAR)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetYears()
        {
            var years = await _context.AcademicYears.OrderByDescending(y => y.Id).ToListAsync();
            return Json(new { data = years });
        }

        [HttpPost]
        public async Task<IActionResult> SaveYear([FromBody] AcademicYearDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            if (model.Id == 0) // Thêm mới
            {
                var year = new AcademicYear { YearName = model.YearName, Description = model.Description };
                _context.AcademicYears.Add(year);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm Khóa học thành công!" });
            }
            else // Cập nhật
            {
                var year = await _context.AcademicYears.FindAsync(model.Id);
                if (year == null) return NotFound(new { message = "Không tìm thấy Khóa học!" });

                year.YearName = model.YearName;
                year.Description = model.Description;
                _context.AcademicYears.Update(year);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật Khóa học thành công!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteYear(int id)
        {
            // Bao gồm Student để kiểm tra xem Khóa này đã có sinh viên chưa
            var year = await _context.AcademicYears.Include(y => y.Students).FirstOrDefaultAsync(y => y.Id == id);
            if (year == null) return NotFound(new { message = "Không tìm thấy Khóa học!" });

            if (year.Students.Any())
                return BadRequest(new { message = "Không thể xóa vì đã có Sinh viên thuộc Khóa này!" });

            _context.AcademicYears.Remove(year);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa Khóa học thành công!" });
        }

        // ==========================================
        // QUẢN LÝ HỌC KỲ (SEMESTER)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetSemesters()
        {
            var semesters = await _context.Semesters.OrderByDescending(s => s.Id).ToListAsync();
            return Json(new { data = semesters });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSemester([FromBody] SemesterDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            if (model.StartDate >= model.EndDate)
                return BadRequest(new { message = "Ngày kết thúc phải lớn hơn ngày bắt đầu!" });

            if (model.Id == 0) // Thêm mới
            {
                var semester = new Semester
                {
                    SemesterName = model.SemesterName,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = true
                };
                _context.Semesters.Add(semester);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm Học kỳ thành công!" });
            }
            else // Cập nhật
            {
                var semester = await _context.Semesters.FindAsync(model.Id);
                if (semester == null) return NotFound(new { message = "Không tìm thấy Học kỳ!" });

                semester.SemesterName = model.SemesterName;
                semester.StartDate = model.StartDate;
                semester.EndDate = model.EndDate;
                // Thuộc tính IsActive được quản lý riêng qua hàm Toggle

                _context.Semesters.Update(semester);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật Học kỳ thành công!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSemesterStatus(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound(new { message = "Không tìm thấy Học kỳ!" });

            semester.IsActive = !semester.IsActive; // Đảo ngược trạng thái
            await _context.SaveChangesAsync();

            var msg = semester.IsActive ? "Đã MỞ lại học kỳ!" : "Đã ĐÓNG học kỳ!";
            return Ok(new { success = true, message = msg });
        }
    }
}