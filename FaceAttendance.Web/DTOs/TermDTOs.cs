using System;
using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class AcademicYearDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        public string YearName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class SemesterDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên học kỳ không được để trống")]
        public string SemesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
    }
}