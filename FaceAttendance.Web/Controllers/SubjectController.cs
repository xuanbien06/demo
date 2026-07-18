using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SubjectController : Controller
    {
        private readonly AppDbContext _context;

        public SubjectController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /Subject/Index -> Trả về giao diện HTML
        public IActionResult Index()
        {
            return View();
        }

        // [GET] /Subject/GetAll -> API Lấy danh sách Môn học
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var subjects = await _context.Subjects.OrderByDescending(s => s.Id).ToListAsync();
            return Json(new { data = subjects });
        }

        // [POST] /Subject/Save -> API Thêm hoặc Sửa
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SubjectDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            // Kiểm tra: Không cho phép trùng Mã Môn Học
            bool isDuplicateCode = await _context.Subjects
                .AnyAsync(s => s.SubjectCode.ToLower() == model.SubjectCode.ToLower() && s.Id != model.Id);

            if (isDuplicateCode)
                return BadRequest(new { message = $"Mã môn học '{model.SubjectCode}' đã tồn tại trong hệ thống!" });

            if (model.Id == 0) // Thêm mới
            {
                var subject = new Subject
                {
                    SubjectCode = model.SubjectCode.ToUpper(), // Chuẩn hóa mã môn viết hoa
                    SubjectName = model.SubjectName,
                    Credits = model.Credits
                };
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm môn học thành công!" });
            }
            else // Cập nhật
            {
                var subject = await _context.Subjects.FindAsync(model.Id);
                if (subject == null)
                    return NotFound(new { message = "Không tìm thấy môn học!" });

                subject.SubjectCode = model.SubjectCode.ToUpper();
                subject.SubjectName = model.SubjectName;
                subject.Credits = model.Credits;

                _context.Subjects.Update(subject);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật môn học thành công!" });
            }
        }

        // [POST] /Subject/Delete -> API Xóa
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Bao gồm Lớp học (Classes) để kiểm tra xem môn này đã được đưa vào giảng dạy chưa
            var subject = await _context.Subjects.Include(s => s.Classes).FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound(new { message = "Không tìm thấy môn học!" });

            // Ràng buộc toàn vẹn: Đã có lớp thì CẤM XÓA
            if (subject.Classes.Any())
                return BadRequest(new { message = "Không thể xóa vì môn học này đã được phân công cho Lớp học!" });

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa môn học thành công!" });
        }
    }
}