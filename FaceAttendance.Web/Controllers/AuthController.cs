using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Services;
using Microsoft.AspNetCore.Mvc;

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
        // Trả về file HTML/Razor chứa giao diện Login/Register
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu User đã đăng nhập rồi mà cố tình vào lại trang Login thì đẩy về Trang Chủ
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // [POST] /Auth/LoginApi
        // API nhận dữ liệu AJAX từ giao diện
        [HttpPost]
        public async Task<IActionResult> LoginApi([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });

            var token = await _authService.LoginAsync(model);
            if (token == null)
                return Unauthorized(new { success = false, message = "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa." });

            // Lưu JWT vào HttpOnly Cookie để bảo mật
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Chống XSS (Javascript không thể đọc được cookie này)
                Secure = true, // Chỉ truyền qua HTTPS (Bảo mật đường truyền)
                SameSite = SameSiteMode.Strict, // Chống CSRF (Chỉ gửi cookie khi request từ cùng 1 domain)
                Expires = model.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("jwt_token", token, cookieOptions);

            return Ok(new { success = true, message = "Đăng nhập thành công!", token });
        }

        // [POST] /Auth/RegisterApi
        // API đăng ký tài khoản
        [HttpPost]
        public async Task<IActionResult> RegisterApi([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu đăng ký không hợp lệ!" });

            var isSuccess = await _authService.RegisterAsync(model);
            if (!isSuccess)
                return BadRequest(new { success = false, message = "Email này đã được sử dụng!" });

            return Ok(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
        }

        // [POST] /Auth/Logout
        // Xóa Cookie để đăng xuất
        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_token");
            return Ok(new { success = true, message = "Đăng xuất thành công!" });
        }
    }
}