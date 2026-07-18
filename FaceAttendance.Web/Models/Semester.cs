using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Semester
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên học kỳ không được để trống")]
        [StringLength(100)]
        public string SemesterName { get; set; } = string.Empty; // Ví dụ: Học kỳ 1 (2026-2027)

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true; // Dùng để đánh dấu học kỳ đang diễn ra

        public ICollection<ClassRoom> Classes { get; set; } = new List<ClassRoom>();
    }
}