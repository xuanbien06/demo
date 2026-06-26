namespace FaceAttendance.Web.Models
{
    public class StudentClass
    {
        public string StudentID { get; set; }
        public Student Student { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; }
    }
}