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
        public ClassRoom ClassRoom { get; set; } = null!; // ĐÃ SỬA

        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;
        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }
}