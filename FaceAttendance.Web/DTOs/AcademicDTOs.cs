using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class FacultyDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khoa không được để trống")]
        public string FacultyName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class MajorDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên ngành không được để trống")]
        public string MajorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Khoa")]
        public int FacultyId { get; set; }
    }
}