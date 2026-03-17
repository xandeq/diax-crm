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
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email_campaign_id = table.Column<long>(type: "bigint", nullable: true),
                    original_subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    generated_subjects_json = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    selected_subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    estimated_open_rate_improvement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    actual_open_rate_improved = table.Column<bool>(type: "bit", nullable: true),
                    email_campaign_id1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_optimizations", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_optimizations_email_campaigns_email_campaign_id1",
                        column: x => x.email_campaign_id1,
                        principalTable: "email_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_email_optimizations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_optimizations_email_campaign_id1",
                table: "email_optimizations",
                column: "email_campaign_id1");

            migrationBuilder.CreateIndex(
                name: "IX_email_optimizations_user_id",
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
