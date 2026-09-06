using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetUserId",
                table: "ActivityLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "ActivityLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "ActivityLogs");
        }
    }
}
