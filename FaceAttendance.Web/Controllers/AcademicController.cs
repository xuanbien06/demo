using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AcademicController : Controller
    {
        private readonly AppDbContext _context;

        public AcademicController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /Academic/Index 
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFaculties()
        {
            var faculties = await _context.Faculties.OrderByDescending(f => f.Id).ToListAsync();
            return Json(new { data = faculties });
        }

        [HttpPost]
        public async Task<IActionResult> SaveFaculty([FromBody] FacultyDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            if (model.Id == 0) // Thêm mới
            {
                var faculty = new Faculty { FacultyName = model.FacultyName, Description = model.Description };
                _context.Faculties.Add(faculty);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm Khoa thành công!" });
            }
            else // Cập nhật
            {
                var faculty = await _context.Faculties.FindAsync(model.Id);
                if (faculty == null) return NotFound(new { message = "Không tìm thấy Khoa!" });

                faculty.FacultyName = model.FacultyName;
                faculty.Description = model.Description;
                _context.Faculties.Update(faculty);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật Khoa thành công!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFaculty(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound(new { message = "Không tìm thấy Khoa!" });

            _context.Faculties.Remove(faculty);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa Khoa thành công!" });
        }
    }
}