using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageGenerationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "image_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_template = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    default_parameters_json = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    thumbnail_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "image_generation_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    parameters_json = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    reference_image_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_generation_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_image_generation_projects_image_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "image_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "generated_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    model_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    revised_prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    storage_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    provider_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    width = table.Column<int>(type: "int", nullable: false),
                    height = table.Column<int>(type: "int", nullable: false),
                    seed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    estimated_cost = table.Column<decimal>(type: "decimal(10,6)", precision: 18, scale: 2, nullable: true),
                    duration_ms = table.Column<int>(type: "int", nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_generated_images_ai_models_model_id",
                        column: x => x.model_id,
                        principalTable: "ai_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_generated_images_ai_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "ai_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_generated_images_image_generation_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "image_generation_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_images_created_at",
                table: "generated_images",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_generated_images_model_id",
                table: "generated_images",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_images_project_id",
                table: "generated_images",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_images_provider_id",
                table: "generated_images",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_images_user_id",
                table: "generated_images",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_image_generation_projects_status",
                table: "image_generation_projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_image_generation_projects_template_id",
                table: "image_generation_projects",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_image_generation_projects_user_id",
                table: "image_generation_projects",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_image_generation_projects_user_id_created_at",
                table: "image_generation_projects",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_image_templates_category",
                table: "image_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_image_templates_is_enabled",
                table: "image_templates",
                column: "is_enabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_images");

            migrationBuilder.DropTable(
                name: "image_generation_projects");

            migrationBuilder.DropTable(
                name: "image_templates");
        }
    }
}
