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

        public async Task<IActionResult> Index()
        {
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.UserCode, // BỔ SUNG: Trả về UserCode để hiển thị trên bảng
                    RoleName = u.Role != null ? u.Role.Name : "N/A",
                    u.RoleId,
                    u.IsActive
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return Json(new { data = users });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest(new { message = "Email này đã tồn tại trong hệ thống!" });

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                RoleId = model.RoleId,
                UserCode = model.UserCode, // BỔ SUNG: Lưu UserCode vào Database
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _authService.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Thêm người dùng thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UserEditDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Dữ liệu không hợp lệ!" });

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                return BadRequest(new { message = "Email này đã được sử dụng bởi tài khoản khác!" });

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.UserCode = model.UserCode; // BỔ SUNG: Cập nhật UserCode
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản của chính mình!" });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var msg = user.IsActive ? "Đã MỞ KHÓA tài khoản!" : "Đã KHÓA tài khoản!";
            return Ok(new { success = true, message = msg });
        }
    }
}