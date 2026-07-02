using System.ComponentModel.DataAnnotations.Schema;

namespace FaceAttendance.Web.Models
{
    public class ClassStudent
    {
        public int ClassID { get; set; }
        [ForeignKey("ClassID")]
        public Class Class { get; set; } = null!;

        [Column(TypeName = "varchar(20)")]
        public string StudentID { get; set; } = string.Empty;
        [ForeignKey("StudentID")]
        public Student Student { get; set; } = null!;
    }
}