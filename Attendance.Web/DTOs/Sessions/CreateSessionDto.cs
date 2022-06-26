using System.ComponentModel.DataAnnotations;

namespace Attendance.Web.DTOs.Sessions
{
    public class CreateSessionDto
    {
        public CreateSessionDto()
        {
            Date = DateTime.Now;
            TimeFrom = new DateTime(Date.Year, Date.Month, Date.Day, Date.Hour, Date.Minute, 0);
            TimeTo = new DateTime(Date.Year, Date.Month, Date.Day, Date.Hour + 2, Date.Minute, 0);
            Date = Date.Date;
        }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Start time")]
        public DateTime TimeFrom { get; set; }
        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "End time")]
        public DateTime TimeTo { get; set; }
        [Required]
        [Display(Name = "Session Subject")]
        public string Subject { get; set; }
        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Required]
        [Display(Name = "Instructor")]
        public string InstructorId { get; set; }
    }

    public class EditSessionDto
    {
        public int Id { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Start time")]
        public DateTime TimeFrom { get; set; }
        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "End time")]
        public DateTime TimeTo { get; set; }
        [Required]
        [Display(Name = "Session Subject")]
        public string Subject { get; set; }
        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Required]
        [Display(Name = "Instructor")]
        public string InstructorId { get; set; }
    }
}
