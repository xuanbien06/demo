using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    [Table("ClassRooms")] // Đã giữ nguyên Mapping Table của bạn
    public class ClassRoom
    {
        [Key]
        public int ClassID { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- CÁC TRƯỜNG LIÊN KẾT HỌC VỤ MỚI (TỪ BƯỚC 7) ---
        public int? SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        public int? SemesterId { get; set; }
        [ForeignKey("SemesterId")]
        public Semester? Semester { get; set; }

        // Phân công giảng viên từ bảng User
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }

        // ---------------------------------------------------

        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }
}