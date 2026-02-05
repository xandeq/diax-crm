using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiRbacModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    supports_list_models = table.Column<bool>(type: "bit", nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "bit", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    model_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    is_discovered = table.Column<bool>(type: "bit", nullable: false),
                    input_cost_hint = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    output_cost_hint = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    max_tokens_hint = table.Column<int>(type: "int", nullable: true),
                    capabilities_json = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_models_ai_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "ai_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_ai_provider_access",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_ai_provider_access", x => new { x.group_id, x.provider_id });
                    table.ForeignKey(
                        name: "FK_group_ai_provider_access_ai_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "ai_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_ai_provider_access_user_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_permissions",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_permissions", x => new { x.group_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_group_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_permissions_user_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group_members", x => new { x.user_id, x.group_id });
                    table.ForeignKey(
                        name: "FK_user_group_members_admin_users_user_id",
                        column: x => x.user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_members_user_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_ai_model_access",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_model_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_ai_model_access", x => new { x.group_id, x.ai_model_id });
                    table.ForeignKey(
                        name: "FK_group_ai_model_access_ai_models_ai_model_id",
                        column: x => x.ai_model_id,
                        principalTable: "ai_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_ai_model_access_user_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_models_provider_id_is_enabled",
                table: "ai_models",
                columns: new[] { "provider_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_models_provider_id_model_key",
                table: "ai_models",
                columns: new[] { "provider_id", "model_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_providers_key",
                table: "ai_providers",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_ai_model_access_ai_model_id",
                table: "group_ai_model_access",
                column: "ai_model_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_ai_provider_access_provider_id",
                table: "group_ai_provider_access",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_permissions_permission_id",
                table: "group_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_key",
                table: "permissions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_group_members_group_id",
                table: "user_group_members",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_key",
                table: "user_groups",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_ai_model_access");

            migrationBuilder.DropTable(
                name: "group_ai_provider_access");

            migrationBuilder.DropTable(
                name: "group_permissions");

            migrationBuilder.DropTable(
                name: "user_group_members");

            migrationBuilder.DropTable(
                name: "ai_models");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropTable(
                name: "ai_providers");
        }
    }
}
