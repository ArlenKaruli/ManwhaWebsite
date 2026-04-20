using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManwhaWebsite.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerAndPopularity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Manhwas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LatestChapter",
                table: "Manhwas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                table: "Manhwas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Manhwas");

            migrationBuilder.DropColumn(
                name: "LatestChapter",
                table: "Manhwas");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Manhwas");
        }
    }
}
