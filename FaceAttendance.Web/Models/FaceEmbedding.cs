using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class FaceEmbedding
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string StudentID { get; set; } = string.Empty;

        [ForeignKey("StudentID")]
        public Student? Student { get; set; }

        [StringLength(500)]
        public string? ImagePath { get; set; } // Đường dẫn lưu ảnh trong thư mục wwwroot

        public string? EmbeddingVector { get; set; } // Vector AI 128D hoặc 512D (Lưu dạng chuỗi JSON)

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}