using FaceAttendance.Web.DTOs;
using FaceAttendance.Web.Models;

namespace FaceAttendance.Web.Services
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginDTO model);
        string HashPassword(User user, string password);
        bool VerifyPassword(User user, string hashedPassword, string providedPassword);
    }
}