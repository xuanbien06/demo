// Đường dẫn: FaceAttendance.Web/Controllers/StudentController.cs
using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FaceRecognitionService _faceService;

        // Tiêm Database và Service gọi AI vào Controller
        public StudentController(AppDbContext context, FaceRecognitionService faceService)
        {
            _context = context;
            _faceService = faceService;
        }

        // 1. Hiển thị danh sách sinh viên (Code cũ)
        [HttpGet]
        public IActionResult Index()
        {
            var students = _context.Students.ToList();
            return View(students);
        }

        // 2. Mở trang Form thêm sinh viên
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Xử lý dữ liệu khi bấm nút Submit trên Form
        [HttpPost]
        public async Task<IActionResult> Create(Student student, IFormFile faceImage)
        {
            try
            {
                // BƯỚC A: LƯU THÔNG TIN CƠ BẢN
                student.IsActive = true;
                _context.Students.Add(student); // Thêm sinh viên vào DB
                await _context.SaveChangesAsync(); // Lưu xuống SQL Server

                // BƯỚC B: XỬ LÝ ẢNH & AI
                if (faceImage != null && faceImage.Length > 0)
                {
                    // Gửi ảnh sang Python API (chạy ở cổng 8000)
                    List<float> vector = await _faceService.GetFaceEmbeddingAsync(faceImage);

                    if (vector != null)
                    {
                        // Hàm JsonSerializer biến List<float> thành chuỗi text ngoặc vuông "[0.1, -0.5, ...]"
                        string vectorJson = System.Text.Json.JsonSerializer.Serialize(vector);

                        // Tạo đối tượng FaceEmbedding mới
                        var embeddingRecord = new FaceEmbedding
                        {
                            StudentID = student.StudentID, // Nối với sinh viên vừa tạo ở trên
                            VectorData = vectorJson,
                            CreatedAt = DateTime.Now
                        };

                        _context.FaceEmbeddings.Add(embeddingRecord); // Thêm khuôn mặt vào DB
                        await _context.SaveChangesAsync(); // Lưu xuống SQL Server
                    }
                }

                // Nếu thành công, điều hướng quay lại trang Danh sách (Index)
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Nếu lỗi (mất mạng, Python sập, lỗi SQL...) thì báo ra màn hình
                ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                return View(student);
            }
        }
        // ==========================================
        // CHỨC NĂNG 1: SỬA THÔNG TIN (EDIT)
        // ==========================================

        // 1. Mở form chứa sẵn thông tin cũ của sinh viên
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Tìm sinh viên trong DB dựa vào Mã SV
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            return View(student);
        }

        // 2. Nhận dữ liệu mới và lưu đè lên DB
        [HttpPost]
        public async Task<IActionResult> Edit(string oldStudentID, Student student, IFormFile faceImage)
        {
            try
            {
                // ===============================================
                // PHẦN A: XỬ LÝ ĐỔI MÃ SINH VIÊN (KHÓA CHÍNH)
                // ===============================================
                if (oldStudentID != student.StudentID)
                {
                    // 1. Kiểm tra xem mã mới định đổi có bị trùng với người khác không
                    var checkExist = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentID == student.StudentID);
                    if (checkExist != null)
                    {
                        ModelState.AddModelError("", "Mã sinh viên mới đã tồn tại, vui lòng chọn mã khác!");
                        return View(student);
                    }

                    // 2. Lấy toàn bộ dữ liệu của người cũ ra
                    var oldStudent = await _context.Students.FindAsync(oldStudentID);
                    var oldFace = await _context.FaceEmbeddings.FirstOrDefaultAsync(f => f.StudentID == oldStudentID);

                    // 3. Xóa người cũ đi
                    if (oldFace != null) _context.FaceEmbeddings.Remove(oldFace);
                    if (oldStudent != null) _context.Students.Remove(oldStudent);
                    await _context.SaveChangesAsync();

                    // 4. Thêm người mới vào với Mã mới
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // 5. Nếu không up ảnh mới, ta chép lại dữ liệu khuôn mặt cũ sang cho Mã mới
                    if (oldFace != null && (faceImage == null || faceImage.Length == 0))
                    {
                        var copyFace = new FaceEmbedding
                        {
                            StudentID = student.StudentID,
                            VectorData = oldFace.VectorData,
                            CreatedAt = oldFace.CreatedAt
                        };
                        _context.FaceEmbeddings.Add(copyFace);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Nếu không đổi Mã SV, chỉ cập nhật thông tin chữ bình thường
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                }

                // ===============================================
                // PHẦN B: XỬ LÝ CẬP NHẬT ẢNH KHUÔN MẶT LÊN AI
                // ===============================================
                if (faceImage != null && faceImage.Length > 0)
                {
                    // Gửi ảnh mới sang Python tính toán
                    List<float> newVector = await _faceService.GetFaceEmbeddingAsync(faceImage);
                    if (newVector != null)
                    {
                        string vectorJson = System.Text.Json.JsonSerializer.Serialize(newVector);
                        var currentFace = await _context.FaceEmbeddings.FirstOrDefaultAsync(f => f.StudentID == student.StudentID);

                        if (currentFace != null)
                        {
                            // Có mặt rồi thì ghi đè Vector mới
                            currentFace.VectorData = vectorJson;
                            currentFace.CreatedAt = DateTime.Now;
                            _context.FaceEmbeddings.Update(currentFace);
                        }
                        else
                        {
                            // Chưa có thì tạo mới
                            var newFace = new FaceEmbedding
                            {
                                StudentID = student.StudentID,
                                VectorData = vectorJson,
                                CreatedAt = DateTime.Now
                            };
                            _context.FaceEmbeddings.Add(newFace);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu: " + ex.Message);
                return View(student);
            }
        }

        // ==========================================
        // CHỨC NĂNG 2: XÓA SINH VIÊN (DELETE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                // LƯU Ý QUAN TRỌNG: Vì có khóa ngoại, ta phải xóa dữ liệu khuôn mặt trước
                var faceData = _context.FaceEmbeddings.FirstOrDefault(f => f.StudentID == id);
                if (faceData != null)
                {
                    _context.FaceEmbeddings.Remove(faceData);
                }

                // Sau đó mới được xóa Sinh viên
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}