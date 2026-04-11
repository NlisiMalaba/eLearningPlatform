using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentArchiveAndCaptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermanentDeletionHangfireJobId",
                table: "ContentItems",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaptionTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptionTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptionTracks_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transcripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transcripts_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaptionTracks_ContentItemId",
                table: "CaptionTracks",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptionTracks_tenant_id",
                table: "CaptionTracks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_CaptionTracks_tenant_id_ContentItemId",
                table: "CaptionTracks",
                columns: new[] { "tenant_id", "ContentItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_ContentItemId",
                table: "Transcripts",
                column: "ContentItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_tenant_id",
                table: "Transcripts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_tenant_id_ContentItemId",
                table: "Transcripts",
                columns: new[] { "tenant_id", "ContentItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptionTracks");

            migrationBuilder.DropTable(
                name: "Transcripts");

            migrationBuilder.DropColumn(
                name: "PermanentDeletionHangfireJobId",
                table: "ContentItems");
        }
    }
}
