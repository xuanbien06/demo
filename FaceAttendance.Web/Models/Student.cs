using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class Student
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string StudentID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        // CHÚ Ý: BẮT BUỘC PHẢI CÓ DÒNG NÀY
        public bool IsActive { get; set; } = true;
    }
}