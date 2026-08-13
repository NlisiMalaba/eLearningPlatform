using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncConflictLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncConflictLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfflineSyncQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServerTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetainedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LocalWon = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflictLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConflictLogs_OfflineSyncQueues_OfflineSyncQueueId",
                        column: x => x.OfflineSyncQueueId,
                        principalTable: "OfflineSyncQueues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflictLogs_OfflineSyncQueueId",
                table: "SyncConflictLogs",
                column: "OfflineSyncQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflictLogs_tenant_id",
                table: "SyncConflictLogs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflictLogs_tenant_id_StudentId_CreatedAt",
                table: "SyncConflictLogs",
                columns: new[] { "tenant_id", "StudentId", "CreatedAt" });

            migrationBuilder.Sql(
                """
                ALTER TABLE "SyncConflictLogs" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "SyncConflictLogs" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncConflictLogs");
        }
    }
}
