using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Web.Data.Entities
{
    [Table("Grades", Schema = "Lookups")]
    public class Grade
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
