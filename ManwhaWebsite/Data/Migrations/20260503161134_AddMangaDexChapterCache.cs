using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManwhaWebsite.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMangaDexChapterCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MdChapters",
                table: "UserReadingLists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MdChaptersCachedAt",
                table: "UserReadingLists",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MdChapters",
                table: "UserReadingLists");

            migrationBuilder.DropColumn(
                name: "MdChaptersCachedAt",
                table: "UserReadingLists");
        }
    }
}
