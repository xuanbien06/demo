using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaceAttendance.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddClassAndAttendanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.ClassID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Classes_ClassID",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassStudents",
                columns: table => new
                {
                    ClassID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassStudents", x => new { x.ClassID, x.StudentID });
                    table.ForeignKey(
                        name: "FK_ClassStudents_Classes_ClassID",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassStudents_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    RecordID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<string>(type: "varchar(20)", nullable: false),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.RecordID);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AttendanceSessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "AttendanceSessions",
                        principalColumn: "SessionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SessionID",
                table: "AttendanceRecords",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentID",
                table: "AttendanceRecords",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_ClassID",
                table: "AttendanceSessions",
                column: "ClassID");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassName",
                table: "Classes",
                column: "ClassName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassStudents_StudentID",
                table: "ClassStudents",
                column: "StudentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "ClassStudents");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");

            migrationBuilder.DropTable(
                name: "Classes");
        }
    }
}
