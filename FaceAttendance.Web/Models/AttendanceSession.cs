using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class AttendanceSession
    {
        [Key]
        public int SessionID { get; set; }

        public int ClassID { get; set; }
        [ForeignKey("ClassID")]
        public Class Class { get; set; } = null!;

        public DateTime Date { get; set; } = DateTime.Now;

        // Đánh dấu phiên này đã kết thúc chưa (Để xử lý vụ gửi Email sau khi kết thúc)
        public bool IsCompleted { get; set; } = false;

        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }
}