using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTimeoutSecondsToTimeoutMs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeoutSeconds",
                table: "TestCases",
                newName: "TimeoutMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeoutMs",
                table: "TestCases",
                newName: "TimeoutSeconds");
        }
    }
}
