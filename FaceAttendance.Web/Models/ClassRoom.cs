using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    [Table("ClassRooms")] // ĐÃ SỬA TỪ "Classes" THÀNH "ClassRooms"
    public class ClassRoom
    {
        [Key]
        public int ClassID { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        public string ClassName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }
}