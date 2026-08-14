using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreschoolSessionSegmentAndInactivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastInteractionAt",
                table: "StudentSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestPromptRequired",
                table: "StudentSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SegmentStartedAt",
                table: "StudentSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastInteractionAt",
                table: "StudentSessions");

            migrationBuilder.DropColumn(
                name: "RestPromptRequired",
                table: "StudentSessions");

            migrationBuilder.DropColumn(
                name: "SegmentStartedAt",
                table: "StudentSessions");
        }
    }
}
