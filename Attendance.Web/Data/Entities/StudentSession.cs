using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Web.Data.Entities
{
    [Table("StudentSessions", Schema = "System")]
    public class StudentSession
    {
        public int Id { get; set; }
        
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        [ForeignKey("Session")]
        public int SessionId { get; set; }

        public Student Student { get; set; }
        public Session Session { get; set; }
    }
}
