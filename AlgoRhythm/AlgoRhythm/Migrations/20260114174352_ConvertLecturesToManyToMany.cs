using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class ConvertLecturesToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Courses_CourseId",
                table: "Lectures");

            migrationBuilder.DropIndex(
                name: "IX_Lectures_CourseId",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Lectures");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "CourseProgresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseLecture",
                columns: table => new
                {
                    CoursesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLecture", x => new { x.CoursesId, x.LecturesId });
                    table.ForeignKey(
                        name: "FK_CourseLecture_Courses_CoursesId",
                        column: x => x.CoursesId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseLecture_Lectures_LecturesId",
                        column: x => x.LecturesId,
                        principalTable: "Lectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgresses_UserId1",
                table: "CourseProgresses",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLecture_LecturesId",
                table: "CourseLecture",
                column: "LecturesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseProgresses_Users_UserId1",
                table: "CourseProgresses",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseProgresses_Users_UserId1",
                table: "CourseProgresses");

            migrationBuilder.DropTable(
                name: "CourseLecture");

            migrationBuilder.DropIndex(
                name: "IX_CourseProgresses_UserId1",
                table: "CourseProgresses");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CourseProgresses");

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Lectures",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_CourseId",
                table: "Lectures",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Courses_CourseId",
                table: "Lectures",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
