using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantBlockDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "Tenants",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedUntil",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BlockedUntil",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Tenants");
        }
    }
}
