using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatrimonioAssets : Migration
    {
        // NOTE (F1 Patrimônio): This migration was hand-cleaned to be strictly ADDITIVE.
        // EF's scaffolder had folded in unrelated MODEL DRIFT (pre-existing un-migrated
        // config changes on other entities) that were DESTRUCTIVE and must NOT ship here:
        //   - video_generation_jobs.reference_image_base64  nvarchar(max) -> nvarchar(256) [truncates base64]
        //   - proposals.description                          nvarchar(max) -> nvarchar(256) NOT NULL [truncates]
        //   - DropIndex IX_task_items_customer_id / IX_EmailEvent_Customer_EventType
        //   - CreateIndex IX_proposals_user_id / IX_meetings_user_id
        // Those were removed. If any are intended, generate a SEPARATE, reviewed migration.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    @class = table.Column<string>(name: "class", type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ownership = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    liquidity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    current_value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_basis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    acquired_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    valuation_source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_valuations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    as_of = table.Column<DateTime>(type: "datetime2", nullable: false),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_valuations", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_valuations_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_valuations_asset_id_as_of",
                table: "asset_valuations",
                columns: new[] { "asset_id", "as_of" });

            migrationBuilder.CreateIndex(
                name: "IX_assets_user_id",
                table: "assets",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_valuations");

            migrationBuilder.DropTable(
                name: "assets");
        }
    }
}
