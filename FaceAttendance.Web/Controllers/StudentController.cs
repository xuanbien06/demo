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
        private readonly FaceCacheService _cacheService;

        public StudentController(AppDbContext context, FaceRecognitionService faceService, FaceCacheService cacheService)
        {
            _context = context;
            _faceService = faceService;
            _cacheService = cacheService;
        }

        // ==========================================
        // YÊU CẦU 5: CẬP NHẬT TÌM KIẾM CHO HÀM INDEX
        // ==========================================
        [HttpGet]
        public IActionResult Index(string searchString)
        {
            // Đưa từ khóa vào ViewBag để hiển thị lại trên Form
            ViewBag.SearchString = searchString;

            // Chuyển sang IQueryable để tối ưu truy vấn Database
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Lọc theo Mã sinh viên HOẶC Họ tên
                query = query.Where(s => s.StudentID.Contains(searchString) || s.FullName.Contains(searchString));
            }

            // Thực thi truy vấn và trả về View
            var students = query.ToList();
            return View(students);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Student student, IFormFile faceImage)
        {
            try
            {
                student.IsActive = true;
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                if (faceImage != null && faceImage.Length > 0)
                {
                    var facesResult = await _faceService.GetFaceEmbeddingAsync(faceImage);

                    if (facesResult != null && facesResult.Count > 0)
                    {
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

                    needReloadCache = true;
                }
                else
                {
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                    needReloadCache = true;
                }

                if (faceImage != null && faceImage.Length > 0)
                {
                    var facesResult = await _faceService.GetFaceEmbeddingAsync(faceImage);

                    if (facesResult != null && facesResult.Count > 0)
                    {
                        List<float> newVector = facesResult[0].Vector;
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
                        needReloadCache = true;
                    }
                }

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

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                // 1. Xóa dữ liệu khuôn mặt (như code cũ của bạn)
                var faceData = _context.FaceEmbeddings.FirstOrDefault(f => f.StudentID == id);
                if (faceData != null)
                {
                    _context.FaceEmbeddings.Remove(faceData);
                }

                // 2. Xóa các bản ghi điểm danh của sinh viên này
                var attendanceRecords = _context.AttendanceRecords.Where(a => a.StudentID == id).ToList();
                if (attendanceRecords.Any())
                {
                    _context.AttendanceRecords.RemoveRange(attendanceRecords);
                }

                // 3. Xóa sinh viên này khỏi các lớp học đã tham gia
                var classStudents = _context.ClassStudents.Where(cs => cs.StudentID == id).ToList();
                if (classStudents.Any())
                {
                    _context.ClassStudents.RemoveRange(classStudents);
                }

                // 4. Cuối cùng mới được xóa sinh viên
                _context.Students.Remove(student);

                // Lưu thay đổi vào DB
                await _context.SaveChangesAsync();
                await _cacheService.LoadFacesIntoMemoryAsync();
            }

            return RedirectToAction("Index");
        }
    }
}