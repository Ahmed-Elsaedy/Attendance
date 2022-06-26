using Attendance.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Attendance.Web.Data
{
    public static class ApplicationDbInitializer
    {
        public static void SeedData(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                SeedRoles(roleManager);
                SeedUsers(userManager);
            }
        }

        private static void SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (roleManager.FindByNameAsync("Admin").Result == null)
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
            }

            if (roleManager.FindByNameAsync("Instructor").Result == null)
            {
                roleManager.CreateAsync(new IdentityRole("Instructor")).Wait();
            }
        }

        private static void SeedUsers(UserManager<ApplicationUser> userManager)
        {
            if (userManager.FindByEmailAsync("admin@system").Result == null)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = "admin@system",
                    Email = "admin@system",
                    Firstname = "Admin",
                    Lastname = "User", 
                };

                IdentityResult result = userManager.CreateAsync(user, "P@$$w0rd").Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Admin").Wait();
                }
            }

            if (userManager.FindByEmailAsync("instructor@system").Result == null)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = "instructor@system",
                    Email = "instructor@system",
                    Firstname = "Instructor",
                    Lastname = "User"
                };

                IdentityResult result = userManager.CreateAsync(user, "P@$$w0rd").Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Instructor").Wait();
                }
            }
        }
    }
}

