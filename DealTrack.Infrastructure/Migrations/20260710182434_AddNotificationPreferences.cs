using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifDailyDigest",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifEmailFollowUps",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifEmailPayments",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifEmailSystem",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifPushFollowUps",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifPushOverdue",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifPushPayments",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifWeeklyReport",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifDailyDigest",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifEmailFollowUps",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifEmailPayments",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifEmailSystem",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifPushFollowUps",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifPushOverdue",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifPushPayments",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NotifWeeklyReport",
                table: "ApplicationUsers");
        }
    }
}
