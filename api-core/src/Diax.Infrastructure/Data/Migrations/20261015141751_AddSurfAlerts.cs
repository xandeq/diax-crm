using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSurfAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "surf_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    weekend_saturday = table.Column<DateOnly>(type: "date", nullable: false),
                    weekend_sunday = table.Column<DateOnly>(type: "date", nullable: false),
                    has_good_window = table.Column<bool>(type: "bit", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    best_window_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    alternatives_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    calendar_conflicts_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    sources_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    raw_payload_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surf_alerts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_surf_alerts_created_at",
                table: "surf_alerts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_surf_alerts_idempotency_key",
                table: "surf_alerts",
                column: "idempotency_key",
                unique: true,
                filter: "[idempotency_key] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "surf_alerts");
        }
    }
}
