using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class Major
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên ngành không được để trống")]
        [StringLength(100)]
        public string MajorName { get; set; } = string.Empty;

        public int FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}