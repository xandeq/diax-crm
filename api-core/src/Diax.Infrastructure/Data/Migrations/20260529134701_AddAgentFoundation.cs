using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "agent_type",
                table: "ai_conversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_pending_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    agent_type = table.Column<int>(type: "int", nullable: false),
                    tool_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    tool_use_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    action_label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    executed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_pending_actions", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_pending_actions");

            migrationBuilder.DropColumn(
                name: "agent_type",
                table: "ai_conversations");
        }
    }
}
