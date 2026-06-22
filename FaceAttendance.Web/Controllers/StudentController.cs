// Đường dẫn: FaceAttendance.Web/Controllers/StudentController.cs
using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;

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
    }
}