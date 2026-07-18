using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaceAttendance.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFaceEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VectorData",
                table: "FaceEmbeddings");

            migrationBuilder.RenameColumn(
                name: "EmbeddingID",
                table: "FaceEmbeddings",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingVector",
                table: "FaceEmbeddings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "FaceEmbeddings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingVector",
                table: "FaceEmbeddings");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "FaceEmbeddings");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FaceEmbeddings",
                newName: "EmbeddingID");

            migrationBuilder.AddColumn<string>(
                name: "VectorData",
                table: "FaceEmbeddings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
