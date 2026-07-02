using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamLeadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamLeadId",
                table: "TenantInvites",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamLeadId",
                table: "ApplicationUsers",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamLeadId",
                table: "TenantInvites");

            migrationBuilder.DropColumn(
                name: "TeamLeadId",
                table: "ApplicationUsers");
        }
    }
}
