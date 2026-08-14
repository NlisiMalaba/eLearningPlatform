using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubjectProgressAndModuleUnlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUnlocked",
                table: "StudentProgresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SubjectProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectProgresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectProgresses_tenant_id",
                table: "SubjectProgresses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectProgresses_tenant_id_StudentId_Subject",
                table: "SubjectProgresses",
                columns: new[] { "tenant_id", "StudentId", "Subject" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE "SubjectProgresses" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_policy ON "SubjectProgresses" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectProgresses");

            migrationBuilder.DropColumn(
                name: "IsUnlocked",
                table: "StudentProgresses");
        }
    }
}
