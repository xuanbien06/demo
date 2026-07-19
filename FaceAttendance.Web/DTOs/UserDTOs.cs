using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class UserCreateDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn quyền")]
        public int RoleId { get; set; }

        // BỔ SUNG: Cho phép truyền UserCode từ giao diện
        public string? UserCode { get; set; }
    }

    public class UserEditDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn quyền")]
        public int RoleId { get; set; }

        // BỔ SUNG: Cho phép cập nhật UserCode
        public string? UserCode { get; set; }
    }
}