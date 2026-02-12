using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    provider_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    model_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tokens_input = table.Column<int>(type: "int", nullable: false),
                    tokens_output = table.Column<int>(type: "int", nullable: false),
                    total_tokens = table.Column<int>(type: "int", nullable: false),
                    cost_estimated = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    request_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_usage_logs_ai_models_model_id",
                        column: x => x.model_id,
                        principalTable: "ai_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_usage_logs_ai_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "ai_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_CreatedAt",
                table: "ai_usage_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_ModelId",
                table: "ai_usage_logs",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_ModelId_CreatedAt",
                table: "ai_usage_logs",
                columns: new[] { "model_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_ProviderId",
                table: "ai_usage_logs",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_ProviderId_CreatedAt",
                table: "ai_usage_logs",
                columns: new[] { "provider_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_UserId_CreatedAt",
                table: "ai_usage_logs",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_logs");
        }
    }
}
