using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Class
    {
        [Key]
        public int ClassID { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        public string ClassName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }
}