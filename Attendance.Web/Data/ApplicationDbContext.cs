using Attendance.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Web.Data;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
         => options.UseSqlite("Data Source=app.db");

    public DbSet<Student> Student { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<StudentSession> StudentSessions { get; set; }
    public DbSet<Attendance.Web.Data.Entities.Department> Department { get; set; }
    public DbSet<Attendance.Web.Data.Entities.Grade> Grade { get; set; }
}
