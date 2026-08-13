using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ZimBotInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZimBotInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Question = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Response = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UsedHintMode = table.Column<bool>(type: "boolean", nullable: false),
                    UsedFallback = table.Column<bool>(type: "boolean", nullable: false),
                    IsLowConfidence = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZimBotInteractions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZimBotInteractions_tenant_id",
                table: "ZimBotInteractions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ZimBotInteractions_tenant_id_StudentId_CreatedAt",
                table: "ZimBotInteractions",
                columns: new[] { "tenant_id", "StudentId", "CreatedAt" });

            migrationBuilder.Sql(
                """
                ALTER TABLE "ZimBotInteractions" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "ZimBotInteractions" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZimBotInteractions");
        }
    }
}
