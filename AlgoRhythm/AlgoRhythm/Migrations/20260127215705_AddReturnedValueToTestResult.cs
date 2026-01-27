using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlgoRhythm.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedValueToTestResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnedValue",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnedValue",
                table: "TestResults");
        }
    }
}
