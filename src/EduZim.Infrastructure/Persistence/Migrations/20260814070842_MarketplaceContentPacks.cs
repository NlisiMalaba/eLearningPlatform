using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduZim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceContentPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TeacherName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RemovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPackAccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPackAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPackAccessRequests_ContentPacks_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "ContentPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentPackItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPackItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPackItems_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentPackItems_ContentPacks_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "ContentPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentPackRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Review = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPackRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPackRatings_ContentPacks_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "ContentPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackAccessRequests_ContentPackId_RequestingTenantId",
                table: "ContentPackAccessRequests",
                columns: new[] { "ContentPackId", "RequestingTenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackAccessRequests_RequestingTenantId",
                table: "ContentPackAccessRequests",
                column: "RequestingTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackAccessRequests_tenant_id",
                table: "ContentPackAccessRequests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackItems_ContentItemId",
                table: "ContentPackItems",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackItems_ContentPackId_ContentItemId",
                table: "ContentPackItems",
                columns: new[] { "ContentPackId", "ContentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackItems_tenant_id",
                table: "ContentPackItems",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackRatings_ContentPackId_UserId",
                table: "ContentPackRatings",
                columns: new[] { "ContentPackId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackRatings_tenant_id",
                table: "ContentPackRatings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPacks_Status",
                table: "ContentPacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPacks_tenant_id",
                table: "ContentPacks",
                column: "tenant_id");

            // Status = 1 is ContentPackStatus.Approved; access Status = 1 is ContentPackAccessStatus.Approved.
            // Approved packs are discoverable across tenants; pending packs stay owner-scoped (or visible when
            // app.current_tenant_id is unset for platform-admin operations).
            migrationBuilder.Sql(
                """
                ALTER TABLE "ContentPacks" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY content_packs_select ON "ContentPacks" FOR SELECT
                    USING (
                        tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                        OR status = 1
                        OR COALESCE(current_setting('app.current_tenant_id', true), '') = ''
                    );
                CREATE POLICY content_packs_modify ON "ContentPacks" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''))
                    WITH CHECK (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));

                ALTER TABLE "ContentPackItems" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY content_pack_items_select ON "ContentPackItems" FOR SELECT
                    USING (
                        tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                        OR EXISTS (
                            SELECT 1 FROM "ContentPackAccessRequests" r
                            WHERE r."ContentPackId" = "ContentPackItems"."ContentPackId"
                              AND r."RequestingTenantId"::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                              AND r."Status" = 1
                        )
                    );
                CREATE POLICY content_pack_items_modify ON "ContentPackItems" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''))
                    WITH CHECK (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));

                ALTER TABLE "ContentPackAccessRequests" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY content_pack_access_requests_isolation ON "ContentPackAccessRequests" FOR ALL
                    USING (
                        tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                        OR "RequestingTenantId"::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                    )
                    WITH CHECK (
                        tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                        OR "RequestingTenantId"::text = COALESCE(current_setting('app.current_tenant_id', true), '')
                    );

                ALTER TABLE "ContentPackRatings" ENABLE ROW LEVEL SECURITY;
                CREATE POLICY content_pack_ratings_select ON "ContentPackRatings" FOR SELECT
                    USING (true);
                CREATE POLICY content_pack_ratings_modify ON "ContentPackRatings" FOR ALL
                    USING (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''))
                    WITH CHECK (tenant_id::text = COALESCE(current_setting('app.current_tenant_id', true), ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentPackAccessRequests");

            migrationBuilder.DropTable(
                name: "ContentPackItems");

            migrationBuilder.DropTable(
                name: "ContentPackRatings");

            migrationBuilder.DropTable(
                name: "ContentPacks");
        }
    }
}
