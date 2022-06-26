using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Web.Data.Migrations
{
    public partial class lookups_names : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "Lookups",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "Lookups",
                table: "Departments");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                schema: "Lookups",
                table: "Grades",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                schema: "Lookups",
                table: "Departments",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "Lookups",
                table: "Grades",
                newName: "NameEn");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "Lookups",
                table: "Departments",
                newName: "NameEn");

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "Lookups",
                table: "Grades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "Lookups",
                table: "Departments",
                type: "TEXT",
                nullable: true);
        }
    }
}
