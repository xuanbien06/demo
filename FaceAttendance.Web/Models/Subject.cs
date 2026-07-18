using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã môn học không được để trống")]
        [StringLength(20)]
        public string SubjectCode { get; set; } = string.Empty; // Ví dụ: PRJ301

        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty; // Ví dụ: Lập trình Java Web

        public int Credits { get; set; } // Số tín chỉ

        public ICollection<ClassRoom> Classes { get; set; } = new List<ClassRoom>();
    }
}