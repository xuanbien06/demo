using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class Student
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string StudentID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // --- ĐÃ ĐỔI TỪ NGÀNH (MAJOR) SANG KHOA (FACULTY) ---
        public int? FacultyId { get; set; }
        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }

        public int? AcademicYearId { get; set; }
        [ForeignKey("AcademicYearId")]
        public AcademicYear? AcademicYear { get; set; }
        // ---------------------------------------------------

        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<FaceEmbedding> FaceEmbeddings { get; set; } = new List<FaceEmbedding>();
    }
}