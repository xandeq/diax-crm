using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatrimonioF2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "investment_opportunities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    @class = table.Column<string>(name: "class", type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    thesis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ease_rank = table.Column<int>(type: "int", nullable: true),
                    score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    suggested_allocation_pct = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    risk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investment_opportunities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "next_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    rationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    suggested_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    target_class = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    linked_task_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_next_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wealth_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    risk_profile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    goal_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    goal_years = table.Column<int>(type: "int", nullable: true),
                    target_allocation_json = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wealth_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investment_opportunities_user_id",
                table: "investment_opportunities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_investment_opportunities_user_id_generated_at",
                table: "investment_opportunities",
                columns: new[] { "user_id", "generated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_next_actions_user_id",
                table: "next_actions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_next_actions_user_id_status",
                table: "next_actions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_wealth_profiles_user_id",
                table: "wealth_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investment_opportunities");

            migrationBuilder.DropTable(
                name: "next_actions");

            migrationBuilder.DropTable(
                name: "wealth_profiles");
        }
    }
}
