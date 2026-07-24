using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanExpiresAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlanExpiresAt",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanExpiresAt",
                table: "Tenants");
        }
    }
}
