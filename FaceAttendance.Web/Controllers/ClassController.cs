using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Controllers
{
    public class ClassController : Controller
    {
        private readonly AppDbContext _context;

        public ClassController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Classes.Include(c => c.ClassStudents).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.ClassName.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            int totalItems = await query.CountAsync();
            var classes = await query.OrderByDescending(c => c.CreatedAt)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(classes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassName")] ClassRoom newClass)
        {
            if (ModelState.IsValid)
            {
                bool isExist = await _context.Classes.AnyAsync(c => c.ClassName.ToLower() == newClass.ClassName.ToLower());
                if (isExist)
                {
                    ModelState.AddModelError("ClassName", "Tên lớp này đã tồn tại trong hệ thống.");
                    return View(newClass);
                }

                newClass.CreatedAt = DateTime.Now;
                _context.Add(newClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(newClass);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            return View(classObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassID,ClassName,CreatedAt")] ClassRoom classObj)
        {
            if (id != classObj.ClassID) return NotFound();

            if (ModelState.IsValid)
            {
                bool isExist = await _context.Classes.AnyAsync(c => c.ClassName.ToLower() == classObj.ClassName.ToLower() && c.ClassID != id);
                if (isExist)
                {
                    ModelState.AddModelError("ClassName", "Tên lớp này đã bị trùng với một lớp khác.");
                    return View(classObj);
                }

                try
                {
                    _context.Update(classObj);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(classObj.ClassID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(classObj);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                .Include(c => c.AttendanceSessions)
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (classObj != null)
            {
                _context.ClassStudents.RemoveRange(classObj.ClassStudents);
                _context.Classes.Remove(classObj);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // YÊU CẦU 4: TRANG CHI TIẾT LỚP HỌC
        // =========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Load thông tin lớp kèm theo danh sách sinh viên của lớp đó
            var classObj = await _context.Classes
                .Include(c => c.ClassStudents)
                    .ThenInclude(cs => cs.Student) // Kết nối sang bảng Sinh viên
                .FirstOrDefaultAsync(m => m.ClassID == id);

            if (classObj == null) return NotFound();

            return View(classObj);
        }

        // =========================================================
        // YÊU CẦU 5: GIAO DIỆN THÊM SINH VIÊN VÀO LỚP (GET)
        // =========================================================
        public async Task<IActionResult> AddStudents(int id, string searchString)
        {
            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            ViewBag.ClassID = classObj.ClassID;
            ViewBag.ClassName = classObj.ClassName;

            // 1. Lấy danh sách ID sinh viên ĐÃ CÓ trong lớp này (để loại trừ)
            var existingStudentIds = await _context.ClassStudents
                .Where(cs => cs.ClassID == id)
                .Select(cs => cs.StudentID)
                .ToListAsync();

            // 2. Lấy danh sách sinh viên CHƯA CÓ trong lớp
            var query = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID));

            // 3. Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.StudentID.Contains(searchString) || s.FullName.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            var availableStudents = await query.ToListAsync();
            return View(availableStudents);
        }

        // =========================================================
        // YÊU CẦU 5: XỬ LÝ LƯU SINH VIÊN ĐƯỢC CHỌN VÀO LỚP (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudents(int id, List<string> selectedStudents)
        {
            if (selectedStudents != null && selectedStudents.Any())
            {
                foreach (var studentId in selectedStudents)
                {
                    // Validate: Tránh trường hợp thêm trùng khóa
                    bool exists = await _context.ClassStudents.AnyAsync(cs => cs.ClassID == id && cs.StudentID == studentId);
                    if (!exists)
                    {
                        _context.ClassStudents.Add(new ClassStudent
                        {
                            ClassID = id,
                            StudentID = studentId
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            // Quay về trang Chi tiết lớp
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Action phụ: Xóa sinh viên khỏi lớp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classId, string studentId)
        {
            var record = await _context.ClassStudents.FirstOrDefaultAsync(x => x.ClassID == classId && x.StudentID == studentId);
            if (record != null)
            {
                _context.ClassStudents.Remove(record);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = classId });
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassID == id);
        }
    }
}