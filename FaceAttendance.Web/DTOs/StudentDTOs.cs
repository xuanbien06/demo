using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class StudentDTO
    {
        [Required(ErrorMessage = "Mã Sinh viên không được để trống")]
        [StringLength(20)]
        public string StudentID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        // Đã sửa thành FacultyId
        public int? FacultyId { get; set; }

        public int? AcademicYearId { get; set; }

        public bool IsEditMode { get; set; }
    }
}