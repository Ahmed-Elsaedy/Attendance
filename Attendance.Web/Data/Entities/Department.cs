using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Web.Data.Entities
{
    [Table("Departments", Schema = "Lookups")]
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
