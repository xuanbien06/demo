using FaceAttendance.Web.Data;
using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FaceAttendance.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<string?> LoginAsync(LoginDTO model)
        {
            // 1. Tìm User theo Email, bắt buộc Include bảng Role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            // 2. Kiểm tra tồn tại, trạng thái hoạt động và Role hợp lệ
            if (user == null || !user.IsActive || user.Role == null)
                return null;

            // 3. CHẶN QUYỀN ĐĂNG NHẬP CỦA SINH VIÊN (RBAC ENFORCEMENT)
            // Lấy tên Role chuyển về chữ thường để so sánh an toàn
            var roleName = user.Role.Name.ToLower();
            if (roleName == "student" || roleName == "sinh viên" || roleName == "sinhvien")
            {
                // Trả về null để Controller chặn lại (Không tiết lộ việc tài khoản tồn tại)
                return null;
            }

            // 4. Xác thực mật khẩu
            if (!VerifyPassword(user, user.PasswordHash, model.Password))
                return null;

            // 5. Tạo và trả về JWT Token
            return GenerateJwtToken(user);
        }

        // ĐÃ XÓA BỎ HÀM RegisterAsync ĐỂ ĐẢM BẢO BẢO MẬT HỆ THỐNG ENTERPRISE

        public string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                
                // Lấy chính xác tên Role từ DB. Tuyệt đối KHÔNG fallback về "Student"
                new Claim(ClaimTypes.Role, user.Role!.Name)
            };

            // BỔ SUNG QUAN TRỌNG: Lưu Mã Giảng Viên (UserCode) vào Token
            // Để sau này Giảng viên đăng nhập, ta biết họ là ai mà load đúng Lịch dạy của họ
            if (!string.IsNullOrEmpty(user.UserCode))
            {
                claims.Add(new Claim("UserCode", user.UserCode));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Token tồn tại trong 7 ngày
                Issuer = _config["Jwt:Issuer"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}