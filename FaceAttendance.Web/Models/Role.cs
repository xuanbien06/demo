using System.ComponentModel.DataAnnotations;

namespace FaceAttendance.Web.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên quyền không được để trống")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation property: Một Role có nhiều Users
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}