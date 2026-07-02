using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class AttendanceRecord
    {
        [Key]
        public int RecordID { get; set; }

        public int SessionID { get; set; }
        [ForeignKey("SessionID")]
        public AttendanceSession Session { get; set; } = null!;

        [Column(TypeName = "varchar(20)")]
        public string StudentID { get; set; } = string.Empty;
        [ForeignKey("StudentID")]
        public Student Student { get; set; } = null!;

        public bool IsPresent { get; set; } = false;

        public DateTime? Time { get; set; } // Giờ điểm danh (nếu có mặt)
    }
}