using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Web.Data.Entities
{
    [Table("Students", Schema = "System")]
    public class Student
    {
        public int Id { get; set; }

        public string Code { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTime Birthdate { get; set; }
        public string PhoneNumber { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }

        public string ProfileImageUrl { get; set; }
        public string ImagesUrls { get; set; }

        public Guid? PersonId { get; set; }

        public ICollection<StudentSession> StudentSession { get; set; }
    }
}
