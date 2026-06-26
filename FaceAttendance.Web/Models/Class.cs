using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } // VD: Công nghệ phần mềm

        [StringLength(50)]
        public string Semester { get; set; } // VD: HK1_2025

        public ICollection<StudentClass> StudentClasses { get; set; }
    }
}