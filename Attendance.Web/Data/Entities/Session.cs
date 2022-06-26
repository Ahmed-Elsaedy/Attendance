using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Web.Data.Entities
{
    [Table("Session", Schema = "System")]
    public class Session
    {
        public int Id { get; set; }
        [Display(Name = "Start time")]
        public DateTime DateFrom { get; set; }
        [Display(Name = "End time")]
        public DateTime DateTo { get; set; }
        [Display(Name = "Session Subject")]
        public string Subject { get; set; }
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }
        public Guid Code { get; set; }

        [ForeignKey("Instructor")]
        public string InstructorId { get; set; }
        public ApplicationUser Instructor { get; set; }
        public ICollection<StudentSession> StudentSession { get; set; }
    }
}
