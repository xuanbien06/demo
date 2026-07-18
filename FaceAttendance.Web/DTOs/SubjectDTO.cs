using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.DTOs
{
    public class SubjectDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã môn học không được để trống")]
        [StringLength(20)]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tín chỉ")]
        [Range(1, 10, ErrorMessage = "Số tín chỉ phải từ 1 đến 10")]
        public int Credits { get; set; }
    }
}