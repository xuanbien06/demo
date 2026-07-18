using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassController : Controller
    {
        private readonly AppDbContext _context;

        public ClassController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // QUẢN LÝ LỚP HỌC CHUNG
        // ==========================================
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var classes = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Teacher)
                .Select(c => new {
                    c.ClassID,
                    c.ClassName,
                    c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.SubjectName : "N/A",
                    c.SemesterId,
                    SemesterName = c.Semester != null ? c.Semester.SemesterName : "N/A",
                    c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : "Chưa phân công",
                    c.CreatedAt
                })
                .OrderByDescending(c => c.ClassID)
                .ToListAsync();

            return Json(new { data = classes });
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownData()
        {
            var subjects = await _context.Subjects.Select(s => new { s.Id, s.SubjectName }).ToListAsync();
            var semesters = await _context.Semesters.Where(s => s.IsActive).Select(s => new { s.Id, s.SemesterName }).ToListAsync();
            var teachers = await _context.Users.Where(u => u.RoleId == 2 && u.IsActive).Select(t => new { t.Id, t.FullName }).ToListAsync();
            return Json(new { subjects, semesters, teachers });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ClassDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            bool isDuplicate = await _context.Classes.AnyAsync(c => c.ClassName.ToLower() == model.ClassName.ToLower() && c.ClassID != model.ClassID);
            if (isDuplicate) return BadRequest(new { message = $"Tên lớp '{model.ClassName}' đã tồn tại!" });

            if (model.ClassID == 0)
            {
                var newClass = new ClassRoom
                {
                    ClassName = model.ClassName,
                    SubjectId = model.SubjectId,
                    SemesterId = model.SemesterId,
                    TeacherId = model.TeacherId,
                    CreatedAt = DateTime.Now
                };
                _context.Classes.Add(newClass);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Tạo lớp học thành công!" });
            }
            else
            {
                var existingClass = await _context.Classes.FindAsync(model.ClassID);
                if (existingClass == null) return NotFound(new { message = "Không tìm thấy Lớp học!" });

                existingClass.ClassName = model.ClassName;
                existingClass.SubjectId = model.SubjectId;
                existingClass.SemesterId = model.SemesterId;
                existingClass.TeacherId = model.TeacherId;

                _context.Classes.Update(existingClass);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật Lớp học thành công!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var classRoom = await _context.Classes
                .Include(c => c.ClassStudents)
                .Include(c => c.AttendanceSessions)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classRoom == null) return NotFound(new { message = "Không tìm thấy Lớp học!" });
            if (classRoom.ClassStudents.Any()) return BadRequest(new { message = "Không thể xóa lớp đã có sinh viên!" });
            if (classRoom.AttendanceSessions.Any()) return BadRequest(new { message = "Không thể xóa lớp đã có dữ liệu điểm danh!" });

            _context.Classes.Remove(classRoom);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa lớp học thành công!" });
        }

        // ==========================================
        // QUẢN LÝ SINH VIÊN TRONG LỚP (BƯỚC 13)
        // ==========================================

        // [GET] /Class/AddStudents?id=...
        [HttpGet]
        public IActionResult AddStudents(int id)
        {
            // Truyền ID của lớp ra ngoài View
            ViewBag.ClassId = id;
            return View();
        }

        // API Lấy Thông tin chung của lớp
        [HttpGet]
        public async Task<IActionResult> GetClassInfo(int id)
        {
            var classInfo = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classInfo == null) return NotFound();

            return Json(new
            {
                classInfo.ClassName,
                SubjectName = classInfo.Subject?.SubjectName ?? "N/A",
                SemesterName = classInfo.Semester?.SemesterName ?? "N/A",
                TeacherName = classInfo.Teacher?.FullName ?? "Chưa phân công"
            });
        }

        // API Lấy danh sách SV ĐÃ CÓ trong lớp
        [HttpGet]
        public async Task<IActionResult> GetEnrolledStudents(int classId)
        {
            var students = await _context.ClassStudents
                .Include(cs => cs.Student)
                .Where(cs => cs.ClassID == classId)
                .Select(cs => new {
                    cs.Student.StudentID,
                    cs.Student.FullName,
                    cs.Student.Email
                })
                .ToListAsync();

            return Json(new { data = students });
        }

        // API Lấy danh sách SV CHƯA CÓ trong lớp (Để thêm vào)
        [HttpGet]
        public async Task<IActionResult> GetAvailableStudents(int classId)
        {
            // Lọc ra ID các sinh viên đã nằm trong lớp này
            var enrolledIds = await _context.ClassStudents
                .Where(cs => cs.ClassID == classId)
                .Select(cs => cs.StudentID)
                .ToListAsync();

            // Tìm các sinh viên đang hoạt động và không nằm trong danh sách enrolledIds
            var available = await _context.Students
                .Where(s => s.IsActive && !enrolledIds.Contains(s.StudentID))
                .Select(s => new {
                    s.StudentID,
                    s.FullName,
                    s.Email
                })
                .ToListAsync();

            return Json(new { data = available });
        }

        // API Thêm danh sách Sinh viên vào lớp
        [HttpPost]
        public async Task<IActionResult> AddStudentsToClass([FromBody] AddStudentToClassDTO model)
        {
            if (model.StudentIds == null || !model.StudentIds.Any())
                return BadRequest(new { message = "Vui lòng chọn ít nhất 1 sinh viên!" });

            var classStudents = model.StudentIds.Select(studentId => new ClassStudent
            {
                ClassID = model.ClassId,
                StudentID = studentId
            }).ToList();

            _context.ClassStudents.AddRange(classStudents);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Thêm sinh viên vào lớp thành công!" });
        }

        // API Xóa 1 Sinh viên khỏi lớp
        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromClass(int classId, string studentId)
        {
            var cs = await _context.ClassStudents
                .FirstOrDefaultAsync(x => x.ClassID == classId && x.StudentID == studentId);

            if (cs == null) return NotFound(new { message = "Không tìm thấy sinh viên trong lớp!" });

            // BẢO VỆ DỮ LIỆU: Kiểm tra xem sinh viên này đã có điểm danh ở lớp này chưa
            bool hasAttendance = await _context.AttendanceRecords
                .Include(a => a.Session) // Sửa chữ AttendanceSession thành Session
                .AnyAsync(a => a.StudentID == studentId && a.Session.ClassID == classId);

            if (hasAttendance)
                return BadRequest(new { message = "Sinh viên đã có dữ liệu điểm danh, KHÔNG THỂ XÓA khỏi lớp!" });

            _context.ClassStudents.Remove(cs);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa sinh viên khỏi lớp!" });
        }
    }
}