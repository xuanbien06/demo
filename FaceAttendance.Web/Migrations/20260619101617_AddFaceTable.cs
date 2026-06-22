using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaceAttendance.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceEmbeddings",
                columns: table => new
                {
                    EmbeddingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<string>(type: "varchar(20)", nullable: false),
                    VectorData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEmbeddings", x => x.EmbeddingID);
                    table.ForeignKey(
                        name: "FK_FaceEmbeddings_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_StudentID",
                table: "FaceEmbeddings",
                column: "StudentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceEmbeddings");
        }
    }
}
