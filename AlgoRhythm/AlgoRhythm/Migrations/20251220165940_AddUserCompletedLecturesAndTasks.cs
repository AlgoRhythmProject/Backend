using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompletedLecturesAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCompletedLectures",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LectureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedLectures", x => new { x.UserId, x.LectureId });
                    table.ForeignKey(
                        name: "FK_UserCompletedLectures_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCompletedLectures_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompletedTasks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedTasks", x => new { x.UserId, x.TaskItemId });
                    table.ForeignKey(
                        name: "FK_UserCompletedTasks_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCompletedTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedLectures_LectureId",
                table: "UserCompletedLectures",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedLectures_UserId",
                table: "UserCompletedLectures",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedTasks_TaskItemId",
                table: "UserCompletedTasks",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedTasks_UserId",
                table: "UserCompletedTasks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCompletedLectures");

            migrationBuilder.DropTable(
                name: "UserCompletedTasks");
        }
    }
}
