using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLectureContentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "LectureContents",
                newName: "HtmlContent");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LectureContents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "LectureContents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LectureContents");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "LectureContents");

            migrationBuilder.RenameColumn(
                name: "HtmlContent",
                table: "LectureContents",
                newName: "Text");
        }
    }
}
