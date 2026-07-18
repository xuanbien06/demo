using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FaceAttendance.Web.Controllers
{
    // BẮT BUỘC: Chỉ có tài khoản mang Role "Admin" mới được phép gọi các API trong này
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public UserController(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // [GET] /User/Index -> Trả về giao diện HTML
        public async Task<IActionResult> Index()
        {
            // Truyền danh sách Roles ra View để làm thẻ <select> khi tạo mới user
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View();
        }

        // [GET] /User/GetAll -> API trả về dữ liệu JSON cho bảng
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.Email,
                    RoleName = u.Role != null ? u.Role.Name : "N/A",
                    u.RoleId,
                    u.IsActive
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return Json(new { data = users });
        }

        // [POST] /User/Create -> API Thêm mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            // Kiểm tra trùng Email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest(new { message = "Email này đã tồn tại trong hệ thống!" });

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                RoleId = model.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Băm mật khẩu an toàn
            user.PasswordHash = _authService.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thêm người dùng thành công!" });
        }

        // [POST] /User/Edit -> API Sửa thông tin
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UserEditDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            // Kiểm tra trùng Email với người khác
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                return BadRequest(new { message = "Email này đã được sử dụng bởi tài khoản khác!" });

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        // [POST] /User/ToggleStatus -> API Khóa / Mở khóa
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            // Ngăn chặn Admin tự khóa tài khoản của chính mình gây kẹt hệ thống
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản của chính mình!" });

            user.IsActive = !user.IsActive; // Đảo ngược trạng thái
            await _context.SaveChangesAsync();

            var msg = user.IsActive ? "Đã MỞ KHÓA tài khoản!" : "Đã KHÓA tài khoản!";
            return Ok(new { success = true, message = msg });
        }
    }
}