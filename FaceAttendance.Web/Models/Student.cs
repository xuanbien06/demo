using System.Collections.Generic;
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

        public bool IsActive { get; set; } = true;

        // --- CODE MỚI THÊM VÀO BÊN DƯỚI ---
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}