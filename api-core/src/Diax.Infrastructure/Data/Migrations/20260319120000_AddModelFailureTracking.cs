using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds failure tracking fields to ai_models and error categorization fields to ai_usage_logs.
    ///
    /// Policy: IsEnabled is NEVER set automatically on failure.
    /// These fields track runtime health for observability only.
    ///
    /// ai_models new columns:
    ///   - consecutive_failure_count  INT (default 0)
    ///   - last_failure_at            DATETIME2 NULL
    ///   - last_success_at            DATETIME2 NULL
    ///   - last_failure_category      NVARCHAR(64) NULL  (AiErrorCategory constant)
    ///   - last_failure_message       NVARCHAR(512) NULL (sanitized short summary)
    ///
    /// ai_usage_logs new columns:
    ///   - error_category             NVARCHAR(64) NULL
    ///   - http_status_code           INT NULL
    /// </summary>
    public partial class AddModelFailureTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- ai_models: failure tracking ---
            migrationBuilder.AddColumn<int>(
                name: "consecutive_failure_count",
                table: "ai_models",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_failure_at",
                table: "ai_models",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_success_at",
                table: "ai_models",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_category",
                table: "ai_models",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_message",
                table: "ai_models",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            // --- ai_usage_logs: error observability ---
            migrationBuilder.AddColumn<string>(
                name: "error_category",
                table: "ai_usage_logs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "http_status_code",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "consecutive_failure_count", table: "ai_models");
            migrationBuilder.DropColumn(name: "last_failure_at", table: "ai_models");
            migrationBuilder.DropColumn(name: "last_success_at", table: "ai_models");
            migrationBuilder.DropColumn(name: "last_failure_category", table: "ai_models");
            migrationBuilder.DropColumn(name: "last_failure_message", table: "ai_models");
            migrationBuilder.DropColumn(name: "error_category", table: "ai_usage_logs");
            migrationBuilder.DropColumn(name: "http_status_code", table: "ai_usage_logs");
        }
    }
}
