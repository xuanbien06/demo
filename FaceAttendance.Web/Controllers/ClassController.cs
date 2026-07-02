using FaceAttendance.Web.Data;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceAttendance.Web.Controllers
{
    public class ClassController : Controller
    {
        private readonly AppDbContext _context;

        public ClassController(AppDbContext context)
        {
            _context = context;
        }

        // YÊU CẦU 1: Giao diện danh sách lớp (Có tìm kiếm, phân trang)
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 10; // Số lượng hiển thị trên 1 trang
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

        // YÊU CẦU 2: Trang thêm lớp (GET)
        public IActionResult Create()
        {
            return View();
        }

        // YÊU CẦU 2: Xử lý lưu lớp mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassName")] Class newClass)
        {
            if (ModelState.IsValid)
            {
                // Validate: Không được trùng tên lớp
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

        // YÊU CẦU 3: Trang sửa lớp (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            return View(classObj);
        }

        // YÊU CẦU 3: Xử lý lưu sửa lớp (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassID,ClassName,CreatedAt")] Class classObj)
        {
            if (id != classObj.ClassID) return NotFound();

            if (ModelState.IsValid)
            {
                // Validate: Không được trùng tên lớp với lớp KHÁC
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

        // YÊU CẦU 3: Nút xóa (Xóa lớp và quan hệ, giữ nguyên sinh viên)
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
                // Xóa quan hệ trong bảng trung gian (Dữ liệu Student gốc không bị ảnh hưởng)
                _context.ClassStudents.RemoveRange(classObj.ClassStudents);
                _context.Classes.Remove(classObj);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassID == id);
        }
    }
}