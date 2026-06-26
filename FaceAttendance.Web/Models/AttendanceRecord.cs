using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class AttendanceRecord
    {
        [Key]
        public int RecordId { get; set; }

        public int SessionId { get; set; }
        public AttendanceSession Session { get; set; }

        public string StudentID { get; set; }
        public Student Student { get; set; }

        public DateTime CheckInTime { get; set; }
        public double ConfidenceScore { get; set; } // Lưu lại độ chính xác để làm minh chứng
    }
}