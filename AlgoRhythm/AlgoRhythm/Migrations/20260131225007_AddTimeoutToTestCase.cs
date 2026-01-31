using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeoutToTestCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeoutMs",
                table: "TestCases");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Timeout",
                table: "TestCases",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timeout",
                table: "TestCases");

            migrationBuilder.AddColumn<int>(
                name: "TimeoutMs",
                table: "TestCases",
                type: "int",
                nullable: true);
        }
    }
}
