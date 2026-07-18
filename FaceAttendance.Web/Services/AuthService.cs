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
            // 1. Tìm User theo Email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            // 2. Kiểm tra tồn tại và trạng thái hoạt động
            if (user == null || !user.IsActive)
                return null;

            // 3. Xác thực mật khẩu
            if (!VerifyPassword(user, user.PasswordHash, model.Password))
                return null;

            // 4. Tạo và trả về JWT Token
            return GenerateJwtToken(user);
        }

        public async Task<bool> RegisterAsync(RegisterDTO model)
        {
            // 1. Check xem Email đã bị đăng ký chưa
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return false;

            // 2. Tạo đối tượng User mới (Mặc định RoleId = 3 là Student)
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                RoleId = 3,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // 3. Hash Password trước khi lưu
            user.PasswordHash = HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

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
                // Lấy tên Role, nếu rỗng thì lấy mặc định là Student
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Student")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Set token tồn tại trong 7 ngày
                Issuer = _config["Jwt:Issuer"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}