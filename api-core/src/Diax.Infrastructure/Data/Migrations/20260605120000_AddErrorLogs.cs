using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "error_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    app_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    level = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    exception_type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    stack_trace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    line_number = table.Column<int>(type: "int", nullable: true),
                    request_method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    request_path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    additional_data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fingerprint = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    occurrence_count = table.Column<int>(type: "int", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_resolved = table.Column<bool>(type: "bit", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    resolution_note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_error_logs_app_level_resolved_date",
                table: "error_logs",
                columns: new[] { "app_name", "level", "is_resolved", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_error_logs_fingerprint",
                table: "error_logs",
                columns: new[] { "fingerprint", "app_name", "is_resolved" },
                filter: "[fingerprint] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_error_logs_occurred_at_app",
                table: "error_logs",
                columns: new[] { "occurred_at", "app_name", "level" });

            migrationBuilder.CreateIndex(
                name: "IX_error_logs_occurred_at_retention",
                table: "error_logs",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "error_logs");
        }
    }
}
