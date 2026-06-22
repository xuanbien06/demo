using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class FaceEmbedding
    {
        [Key]
        public int EmbeddingID { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string StudentID { get; set; } = string.Empty;

        [ForeignKey("StudentID")]
        public Student Student { get; set; } = null!; // EF sẽ gán, =null! để loại warning nullable

        [Required]
        public string VectorData { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}