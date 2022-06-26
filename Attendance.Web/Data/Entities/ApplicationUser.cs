using Microsoft.AspNetCore.Identity;

namespace Attendance.Web.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }

        public string DisplayName
        {
            get { return string.Format("{0} {1}", Firstname, Lastname); }
        }
    }
}
