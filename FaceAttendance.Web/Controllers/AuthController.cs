using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FaceAttendance.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // [GET] /Auth/Login
        // Trả về file HTML/Razor chứa giao diện Login (Giao diện này sắp tới bạn cũng cần bỏ tab Đăng ký đi)
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu User đã đăng nhập rồi mà cố tình vào lại trang Login thì đẩy về Dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // [POST] /Auth/LoginApi
        // API nhận dữ liệu từ form đăng nhập
        [HttpPost]
        public async Task<IActionResult> LoginApi([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });

            // AuthService bây giờ chỉ phục vụ Admin và Giảng viên
            var token = await _authService.LoginAsync(model);
            if (token == null)
                return Unauthorized(new { success = false, message = "Email hoặc mật khẩu không chính xác, hoặc tài khoản bị khóa." });

            // Bảo mật JWT bằng HttpOnly Cookie (Rất tốt, tiếp tục phát huy)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = model.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("jwt_token", token, cookieOptions);

            return Ok(new { success = true, message = "Đăng nhập thành công!", token });
        }

        // [POST] /Auth/Logout
        // Hủy session và cookie
        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_token");
            return Ok(new { success = true, message = "Đăng xuất thành công!" });
        }
    }
}