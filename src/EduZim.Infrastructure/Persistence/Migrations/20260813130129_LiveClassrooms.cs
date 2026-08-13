using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveClassrooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomParticipants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RoomId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordingUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_tenant_id",
                table: "AttendanceRecords",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_tenant_id_ClassroomSessionId_StudentUserId",
                table: "AttendanceRecords",
                columns: new[] { "tenant_id", "ClassroomSessionId", "StudentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomParticipants_tenant_id",
                table: "ClassroomParticipants",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomParticipants_tenant_id_ClassroomSessionId_UserId",
                table: "ClassroomParticipants",
                columns: new[] { "tenant_id", "ClassroomSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomSessions_tenant_id",
                table: "ClassroomSessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomSessions_tenant_id_SchoolClassId_StartAtUtc",
                table: "ClassroomSessions",
                columns: new[] { "tenant_id", "SchoolClassId", "StartAtUtc" });

            migrationBuilder.Sql(
                """
                ALTER TABLE "ClassroomSessions" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "ClassroomSessions" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));

                ALTER TABLE "ClassroomParticipants" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "ClassroomParticipants" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));

                ALTER TABLE "AttendanceRecords" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "AttendanceRecords" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "ClassroomParticipants");

            migrationBuilder.DropTable(
                name: "ClassroomSessions");
        }
    }
}
