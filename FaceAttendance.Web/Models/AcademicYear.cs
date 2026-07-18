using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class AcademicYear
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [StringLength(50)]
        public string YearName { get; set; } = string.Empty; // Ví dụ: Khóa 15 (K15)

        [StringLength(255)]
        public string? Description { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}