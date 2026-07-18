using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaceAttendance.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduleFromClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Schedule",
                table: "ClassRooms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                table: "ClassRooms",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
