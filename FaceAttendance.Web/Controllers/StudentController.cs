using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var students = await _context.Students
                .Include(s => s.Faculty) // Sửa thành Include Faculty
                .Include(s => s.AcademicYear)
                .Select(s => new {
                    s.StudentID,
                    s.FullName,
                    s.Email,
                    s.IsActive,
                    FacultyId = s.FacultyId,
                    FacultyName = s.Faculty != null ? s.Faculty.FacultyName : "Chưa xếp Khoa", // Sửa thành FacultyName
                    AcademicYearId = s.AcademicYearId,
                    YearName = s.AcademicYear != null ? s.AcademicYear.YearName : "Chưa xếp Khóa"
                })
                .OrderByDescending(s => s.StudentID)
                .ToListAsync();

            return Json(new { data = students });
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownData()
        {
            // Sửa thành lấy danh sách Khoa (Faculties)
            var faculties = await _context.Faculties.Select(f => new { f.Id, f.FacultyName }).ToListAsync();
            var years = await _context.AcademicYears.Select(y => new { y.Id, y.YearName }).ToListAsync();
            return Json(new { faculties, years });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] StudentDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            bool isEmailExist = await _context.Students
                .AnyAsync(s => s.Email.ToLower() == model.Email.ToLower() && s.StudentID != model.StudentID);
            if (isEmailExist)
                return BadRequest(new { message = "Email này đã được sử dụng bởi sinh viên khác!" });

            if (!model.IsEditMode)
            {
                bool isIdExist = await _context.Students.AnyAsync(s => s.StudentID.ToUpper() == model.StudentID.ToUpper());
                if (isIdExist)
                    return BadRequest(new { message = $"Mã sinh viên '{model.StudentID}' đã tồn tại!" });

                var student = new Student
                {
                    StudentID = model.StudentID.ToUpper(),
                    FullName = model.FullName,
                    Email = model.Email,
                    FacultyId = model.FacultyId, // Sửa thành FacultyId
                    AcademicYearId = model.AcademicYearId,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm sinh viên thành công!" });
            }
            else
            {
                var student = await _context.Students.FindAsync(model.StudentID);
                if (student == null)
                    return NotFound(new { message = "Không tìm thấy Sinh viên!" });

                student.FullName = model.FullName;
                student.Email = model.Email;
                student.FacultyId = model.FacultyId; // Sửa thành FacultyId
                student.AcademicYearId = model.AcademicYearId;

                _context.Students.Update(student);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật sinh viên thành công!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Không tìm thấy Sinh viên!" });

            student.IsActive = !student.IsActive;
            await _context.SaveChangesAsync();

            var msg = student.IsActive ? "Đã MỞ KHÓA sinh viên!" : "Đã KHÓA sinh viên!";
            return Ok(new { success = true, message = msg });
        }
    }
}