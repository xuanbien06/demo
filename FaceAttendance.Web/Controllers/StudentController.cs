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
        private readonly FaceCacheService _cacheService; // Bổ sung Cache Service

        // Tiêm Database, AI Service và Cache Service vào Controller
        public StudentController(AppDbContext context, FaceRecognitionService faceService, FaceCacheService cacheService)
        {
            _context = context;
            _faceService = faceService;
            _cacheService = cacheService;
        }

        // 1. Hiển thị danh sách sinh viên
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
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // BƯỚC B: XỬ LÝ ẢNH & AI
                if (faceImage != null && faceImage.Length > 0)
                {
                    // Đã sửa: Hứng kết quả là List các khuôn mặt từ AI
                    var facesResult = await _faceService.GetFaceEmbeddingAsync(faceImage);

                    // Kiểm tra xem AI có tìm thấy khuôn mặt nào không
                    if (facesResult != null && facesResult.Count > 0)
                    {
                        // Lấy Vector của khuôn mặt đầu tiên (vì ảnh thẻ đăng ký thường chỉ có 1 người)
                        List<float> vector = facesResult[0].Vector;

                        string vectorJson = System.Text.Json.JsonSerializer.Serialize(vector);

                        var embeddingRecord = new FaceEmbedding
                        {
                            StudentID = student.StudentID,
                            VectorData = vectorJson,
                            CreatedAt = DateTime.Now
                        };

                        _context.FaceEmbeddings.Add(embeddingRecord);
                        await _context.SaveChangesAsync();

                        // [QUAN TRỌNG] Nạp lại bộ nhớ RAM ngay lập tức để AI nhận diện được người mới này!
                        await _cacheService.LoadFacesIntoMemoryAsync();
                    }
                    else
                    {
                        ModelState.AddModelError("", "AI không tìm thấy khuôn mặt nào trong ảnh đăng ký. Vui lòng chụp lại ảnh rõ hơn.");
                        return View(student);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                return View(student);
            }
        }

        // ==========================================
        // CHỨC NĂNG 1: SỬA THÔNG TIN (EDIT)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string oldStudentID, Student student, IFormFile faceImage)
        {
            try
            {
                bool needReloadCache = false;

                // PHẦN A: XỬ LÝ ĐỔI MÃ SINH VIÊN
                if (oldStudentID != student.StudentID)
                {
                    var checkExist = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentID == student.StudentID);
                    if (checkExist != null)
                    {
                        ModelState.AddModelError("", "Mã sinh viên mới đã tồn tại, vui lòng chọn mã khác!");
                        return View(student);
                    }

                    var oldStudent = await _context.Students.FindAsync(oldStudentID);
                    var oldFace = await _context.FaceEmbeddings.FirstOrDefaultAsync(f => f.StudentID == oldStudentID);

                    if (oldFace != null) _context.FaceEmbeddings.Remove(oldFace);
                    if (oldStudent != null) _context.Students.Remove(oldStudent);
                    await _context.SaveChangesAsync();

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

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

                    needReloadCache = true; // Đổi mã SV (ảnh hưởng tới Tên/ID) nên cần nạp lại RAM
                }
                else
                {
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();

                    // Nếu sửa thông tin tên, cũng cần update RAM
                    needReloadCache = true;
                }

                // PHẦN B: XỬ LÝ CẬP NHẬT ẢNH KHUÔN MẶT LÊN AI
                if (faceImage != null && faceImage.Length > 0)
                {
                    // Đã sửa: Hứng kết quả là mảng Box & Vector
                    var facesResult = await _faceService.GetFaceEmbeddingAsync(faceImage);

                    if (facesResult != null && facesResult.Count > 0)
                    {
                        List<float> newVector = facesResult[0].Vector; // Lấy khuôn mặt đầu tiên
                        string vectorJson = System.Text.Json.JsonSerializer.Serialize(newVector);

                        var currentFace = await _context.FaceEmbeddings.FirstOrDefaultAsync(f => f.StudentID == student.StudentID);

                        if (currentFace != null)
                        {
                            currentFace.VectorData = vectorJson;
                            currentFace.CreatedAt = DateTime.Now;
                            _context.FaceEmbeddings.Update(currentFace);
                        }
                        else
                        {
                            var newFace = new FaceEmbedding
                            {
                                StudentID = student.StudentID,
                                VectorData = vectorJson,
                                CreatedAt = DateTime.Now
                            };
                            _context.FaceEmbeddings.Add(newFace);
                        }
                        await _context.SaveChangesAsync();
                        needReloadCache = true; // Có ảnh mới phải nạp lại RAM
                    }
                }

                // Nạp lại RAM nếu có thay đổi
                if (needReloadCache)
                {
                    await _cacheService.LoadFacesIntoMemoryAsync();
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
                var faceData = _context.FaceEmbeddings.FirstOrDefault(f => f.StudentID == id);
                if (faceData != null)
                {
                    _context.FaceEmbeddings.Remove(faceData);
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                // Xóa sinh viên xong cũng phải xóa khỏi RAM Cache
                await _cacheService.LoadFacesIntoMemoryAsync();
            }

            return RedirectToAction("Index");
        }
    }
}