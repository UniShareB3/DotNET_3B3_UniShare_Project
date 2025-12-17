using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureReportDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ModeratorId",
                table: "Reports");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ModeratorId",
                table: "Reports",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ModeratorId",
                table: "Reports");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ModeratorId",
                table: "Reports",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
