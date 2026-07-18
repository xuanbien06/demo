using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Faculty
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khoa không được để trống")]
        [StringLength(100)]
        public string FacultyName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public ICollection<Major> Majors { get; set; } = new List<Major>();
    }
}