using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoProviderLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "aspect_ratio",
                table: "ai_usage_logs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "generation_duration_seconds",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quota_credits_used",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "ai_usage_logs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "video_height",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "video_width",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_text_provider",
                table: "ai_providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_video_provider",
                table: "ai_providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "ai_models",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_duration_seconds",
                table: "ai_models",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "max_resolution",
                table: "ai_models",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supported_aspect_ratios",
                table: "ai_models",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_provider_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_provider_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    daily_generation_limit = table.Column<int>(type: "int", nullable: true),
                    daily_credits_limit = table.Column<int>(type: "int", nullable: true),
                    monthly_generation_limit = table.Column<int>(type: "int", nullable: true),
                    daily_cost_limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    last_reset_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    current_daily_usage = table.Column<int>(type: "int", nullable: false),
                    current_daily_cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    quota_type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    reset_frequency = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    is_enforced = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_provider_quotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_provider_quotas_ai_providers_ai_provider_id",
                        column: x => x.ai_provider_id,
                        principalTable: "ai_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_quotas_ai_provider_id",
                table: "ai_provider_quotas",
                column: "ai_provider_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_provider_quotas");

            migrationBuilder.DropColumn(
                name: "aspect_ratio",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "generation_duration_seconds",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "quota_credits_used",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "video_height",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "video_width",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "is_text_provider",
                table: "ai_providers");

            migrationBuilder.DropColumn(
                name: "is_video_provider",
                table: "ai_providers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "max_duration_seconds",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "max_resolution",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "supported_aspect_ratios",
                table: "ai_models");
        }
    }
}
