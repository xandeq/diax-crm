using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prompt_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    icon = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    collection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    kind = table.Column<byte>(type: "tinyint", nullable: false),
                    framework = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    target_model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    current_version_number = table.Column<int>(type: "int", nullable: false),
                    current_content_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    preview = table.Column<string>(type: "nvarchar(280)", maxLength: 280, nullable: false),
                    variable_names = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_favorite = table.Column<bool>(type: "bit", nullable: false),
                    is_archived = table.Column<bool>(type: "bit", nullable: false),
                    expose_to_mcp = table.Column<bool>(type: "bit", nullable: false),
                    source = table.Column<byte>(type: "tinyint", nullable: false),
                    source_ref_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    use_count = table.Column<int>(type: "int", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompts", x => x.id);
                    table.ForeignKey(
                        name: "FK_prompts_prompt_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "prompt_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "prompt_tags",
                columns: table => new
                {
                    prompt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_tags", x => new { x.prompt_id, x.name });
                    table.ForeignKey(
                        name: "FK_prompt_tags_prompts_prompt_id",
                        column: x => x.prompt_id,
                        principalTable: "prompts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    number = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    variables_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    change_note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    content_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    source = table.Column<byte>(type: "tinyint", nullable: false),
                    framework_applied = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_prompt_versions_prompts_prompt_id",
                        column: x => x.prompt_id,
                        principalTable: "prompts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_collections_user_id",
                table: "prompt_collections",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_collections_user_id_name",
                table: "prompt_collections",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_tags_name",
                table: "prompt_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_prompt_id_number",
                table: "prompt_versions",
                columns: new[] { "prompt_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompts_collection_id",
                table: "prompts",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "IX_prompts_user_id",
                table: "prompts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_prompts_user_id_collection_id",
                table: "prompts",
                columns: new[] { "user_id", "collection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_prompts_user_id_is_archived_updated_at",
                table: "prompts",
                columns: new[] { "user_id", "is_archived", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_prompts_user_id_slug",
                table: "prompts",
                columns: new[] { "user_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompts_user_id_source_source_ref_id",
                table: "prompts",
                columns: new[] { "user_id", "source", "source_ref_id" },
                unique: true,
                filter: "[source_ref_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prompt_tags");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropTable(
                name: "prompts");

            migrationBuilder.DropTable(
                name: "prompt_collections");
        }
    }
}
