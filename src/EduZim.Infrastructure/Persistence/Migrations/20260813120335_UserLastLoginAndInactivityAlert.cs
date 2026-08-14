using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserLastLoginAndInactivityAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastInactivityAlertAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_tenant_id_Role_LastLoginAt",
                table: "AspNetUsers",
                columns: new[] { "tenant_id", "Role", "LastLoginAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_tenant_id_Role_LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastInactivityAlertAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");
        }
    }
}
