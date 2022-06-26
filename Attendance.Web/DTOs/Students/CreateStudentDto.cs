using System.ComponentModel.DataAnnotations;

namespace Attendance.Web.DTOs.Students
{
    public class CreateStudentDto
    {
        [Required]
        public string Code { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        [DataType(DataType.Date)]
        public DateTime Birthdate { get; set; }
        [Required]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        [Required]
        [Display(Name = "Grade")]
        public int GradeId { get; set; }

        [Required]
        [Display(Name = "Profile Image")]
        public IFormFile ProfileImage { get; set; }

        public List<IFormFile> Images { get; set; }

        [Range(5, 10, ErrorMessage = "Images total count must be 5 images")]
        public int ImagesCount { get; set; }
    }
}
