using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class AttendanceSession
    {
        [Key]
        public int SessionId { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; }

        public DateTime Date { get; set; }
        public bool IsActive { get; set; } = true; // Trạng thái buổi học đang diễn ra
    }
}