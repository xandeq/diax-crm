using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPromptsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    original_input = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    generated_prompt = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: false),
                    prompt_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_prompts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_prompts_user_id",
                table: "user_prompts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_prompts_user_id_created_at",
                table: "user_prompts",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_prompts");
        }
    }
}
