using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class FaceAIController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FaceAIController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env; // Dùng để lấy đường dẫn thư mục wwwroot
        }

        // [GET] /FaceAI/Dataset?studentId=...
        [HttpGet]
        public async Task<IActionResult> Dataset(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return RedirectToAction("Index", "Student");

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.StudentID == studentId);

            if (student == null) return NotFound("Không tìm thấy sinh viên");

            ViewBag.Student = student;
            return View();
        }

        // [GET] Lấy danh sách ảnh đã lưu của sinh viên
        [HttpGet]
        public async Task<IActionResult> GetImages(string studentId)
        {
            var images = await _context.FaceEmbeddings
                .Where(f => f.StudentID == studentId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new { f.Id, f.ImagePath, f.CreatedAt })
                .ToListAsync();

            return Json(new { data = images });
        }

        // [POST] Upload ảnh từ File hoặc Webcam (Base64)
        [HttpPost]
        public async Task<IActionResult> UploadFace(string studentId, IFormFile? file, string? base64Image)
        {
            if (string.IsNullOrEmpty(studentId)) return BadRequest(new { message = "Thiếu thông tin sinh viên" });

            // 1. Tạo thư mục chứa ảnh nếu chưa có
            string folderPath = Path.Combine(_env.WebRootPath, "uploads", "dataset", studentId);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}.jpg";
            string filePath = Path.Combine(folderPath, fileName);
            string dbPath = $"/uploads/dataset/{studentId}/{fileName}"; // Đường dẫn lưu vào DB

            try
            {
                // 2. Xử lý lưu file
                if (file != null && file.Length > 0)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else if (!string.IsNullOrEmpty(base64Image))
                {
                    // Convert từ ảnh chụp Webcam (Base64) sang file JPG
                    var base64Data = base64Image.Split(',')[1];
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                }
                else
                {
                    return BadRequest(new { message = "Vui lòng cung cấp ảnh!" });
                }

                // TODO: Chỗ này sau này chúng ta sẽ gọi Python API api.py để trích xuất Embedding Vector.
                // Hiện tại lưu ảnh vào Database trước để hoàn thiện luồng UI.

                // 3. Lưu vào Database
                var faceDb = new FaceEmbedding
                {
                    StudentID = studentId,
                    ImagePath = dbPath,
                    CreatedAt = DateTime.Now
                };

                _context.FaceEmbeddings.Add(faceDb);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã lưu khuôn mặt thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lưu ảnh: " + ex.Message });
            }
        }

        // [POST] Xóa ảnh
        [HttpPost]
        public async Task<IActionResult> DeleteFace(int id)
        {
            var face = await _context.FaceEmbeddings.FindAsync(id);
            if (face == null) return NotFound(new { message = "Không tìm thấy dữ liệu ảnh!" });

            try
            {
                // Xóa file vật lý trong thư mục wwwroot
                if (!string.IsNullOrEmpty(face.ImagePath))
                {
                    string fullPath = Path.Combine(_env.WebRootPath, face.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Xóa record trong DB
                _context.FaceEmbeddings.Remove(face);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã xóa ảnh thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xóa ảnh: " + ex.Message });
            }
        }
    }
}