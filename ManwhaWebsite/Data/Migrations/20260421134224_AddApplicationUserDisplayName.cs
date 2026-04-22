using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManwhaWebsite.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChapterCount",
                table: "Manhwas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genres",
                table: "Manhwas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChapterCount",
                table: "Manhwas");

            migrationBuilder.DropColumn(
                name: "Genres",
                table: "Manhwas");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");
        }
    }
}
