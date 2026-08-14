using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudentSessionsAndScreenTimeLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyScreenTimeLimitSeconds",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccumulatedSeconds = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSessions_tenant_id",
                table: "StudentSessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSessions_tenant_id_StudentId_SessionDate",
                table: "StudentSessions",
                columns: new[] { "tenant_id", "StudentId", "SessionDate" });

            migrationBuilder.Sql(
                """
                ALTER TABLE "StudentSessions" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "StudentSessions" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentSessions");

            migrationBuilder.DropColumn(
                name: "DailyScreenTimeLimitSeconds",
                table: "AspNetUsers");
        }
    }
}
