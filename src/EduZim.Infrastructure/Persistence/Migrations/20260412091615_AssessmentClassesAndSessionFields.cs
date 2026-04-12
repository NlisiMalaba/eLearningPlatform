using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentClassesAndSessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "AssessmentAttempts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentClassAssignmentId",
                table: "AssessmentAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "AssessmentAttempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TimedAutoSubmitHangfireJobId",
                table: "AssessmentAttempts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "AssessmentAttempts" SET "StartedAt" = "CreatedAt" WHERE "SubmittedAt" IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "SchoolClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentClassAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentClassAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentClassAssignments_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentClassAssignments_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassEnrollments_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_AssessmentClassAssignmentId",
                table: "AssessmentAttempts",
                column: "AssessmentClassAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentClassAssignments_AssessmentId_SchoolClassId",
                table: "AssessmentClassAssignments",
                columns: new[] { "AssessmentId", "SchoolClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentClassAssignments_SchoolClassId",
                table: "AssessmentClassAssignments",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentClassAssignments_tenant_id",
                table: "AssessmentClassAssignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ClassEnrollments_SchoolClassId_StudentUserId",
                table: "ClassEnrollments",
                columns: new[] { "SchoolClassId", "StudentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassEnrollments_tenant_id",
                table: "ClassEnrollments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_tenant_id",
                table: "SchoolClasses",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentAttempts_AssessmentClassAssignments_AssessmentCla~",
                table: "AssessmentAttempts",
                column: "AssessmentClassAssignmentId",
                principalTable: "AssessmentClassAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentAttempts_AssessmentClassAssignments_AssessmentCla~",
                table: "AssessmentAttempts");

            migrationBuilder.DropTable(
                name: "AssessmentClassAssignments");

            migrationBuilder.DropTable(
                name: "ClassEnrollments");

            migrationBuilder.DropTable(
                name: "SchoolClasses");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentAttempts_AssessmentClassAssignmentId",
                table: "AssessmentAttempts");

            migrationBuilder.DropColumn(
                name: "AssessmentClassAssignmentId",
                table: "AssessmentAttempts");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "AssessmentAttempts");

            migrationBuilder.DropColumn(
                name: "TimedAutoSubmitHangfireJobId",
                table: "AssessmentAttempts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "AssessmentAttempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
