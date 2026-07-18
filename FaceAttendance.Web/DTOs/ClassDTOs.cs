using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class ClassDTO
    {
        public int ClassID { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Môn học")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Học kỳ")]
        public int SemesterId { get; set; }

        public int? TeacherId { get; set; }
    }

    // DTO để nhận dữ liệu một mảng các sinh viên được chọn thêm vào lớp
    public class AddStudentToClassDTO
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        public List<string> StudentIds { get; set; } = new List<string>();
    }
}