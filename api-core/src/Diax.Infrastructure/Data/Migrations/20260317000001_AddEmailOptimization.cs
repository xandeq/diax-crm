using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_optimizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email_campaign_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    original_subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    generated_subjects_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    selected_subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    estimated_open_rate_improvement = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    actual_open_rate_improved = table.Column<bool>(type: "bit", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_optimizations", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_optimizations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_optimizations_email_campaigns_email_campaign_id",
                        column: x => x.email_campaign_id,
                        principalTable: "email_campaigns",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_optimizations_email_campaign_id",
                table: "email_optimizations",
                column: "email_campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_optimizations_user_id",
                table: "email_optimizations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_optimizations");
        }
    }
}
