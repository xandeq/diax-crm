using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialPlannerModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_card_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    optimal_purchase_day = table.Column<int>(type: "int", nullable: false),
                    maximum_cycle_length = table.Column<int>(type: "int", nullable: false),
                    available_limit_percentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    last_calculated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_recommended = table.Column<bool>(type: "bit", nullable: false),
                    closing_day = table.Column<int>(type: "int", nullable: false),
                    due_day = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_strategies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    target_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    current_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    target_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    category = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    auto_allocate_surplus = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_goals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_simulations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reference_month = table.Column<int>(type: "int", nullable: false),
                    reference_year = table.Column<int>(type: "int", nullable: false),
                    simulation_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    starting_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    projected_ending_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_projected_income = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_projected_expenses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    has_negative_balance_risk = table.Column<bool>(type: "bit", nullable: false),
                    first_negative_balance_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lowest_projected_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_simulations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    frequency_type = table.Column<int>(type: "int", nullable: false),
                    day_of_month = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    payment_method = table.Column<int>(type: "int", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_balance_projections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    simulation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    opening_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_income = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_expenses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    closing_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_negative = table.Column<bool>(type: "bit", nullable: false),
                    has_high_priority_expense = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_balance_projections", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_balance_projections_monthly_simulations_simulation_id",
                        column: x => x.simulation_id,
                        principalTable: "monthly_simulations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projected_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    simulation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    payment_method = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    source = table.Column<int>(type: "int", nullable: false),
                    source_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    actual_transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projected_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_projected_transactions_monthly_simulations_simulation_id",
                        column: x => x.simulation_id,
                        principalTable: "monthly_simulations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    simulation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    message = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    actionable_transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    suggested_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    suggested_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    suggested_credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_recommendations", x => x.id);
                    table.ForeignKey(
                        name: "FK_simulation_recommendations_monthly_simulations_simulation_id",
                        column: x => x.simulation_id,
                        principalTable: "monthly_simulations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_simulation_recommendations_projected_transactions_actionable_transaction_id",
                        column: x => x.actionable_transaction_id,
                        principalTable: "projected_transactions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_strategies_user_id",
                table: "credit_card_strategies",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_balance_projections_simulation_id",
                table: "daily_balance_projections",
                column: "simulation_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_goals_user_id",
                table: "financial_goals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_simulations_user_id",
                table: "monthly_simulations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projected_transactions_simulation_id",
                table: "projected_transactions",
                column: "simulation_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_transactions_user_id",
                table: "recurring_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_recommendations_actionable_transaction_id",
                table: "simulation_recommendations",
                column: "actionable_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_recommendations_simulation_id",
                table: "simulation_recommendations",
                column: "simulation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_card_strategies");

            migrationBuilder.DropTable(
                name: "daily_balance_projections");

            migrationBuilder.DropTable(
                name: "financial_goals");

            migrationBuilder.DropTable(
                name: "recurring_transactions");

            migrationBuilder.DropTable(
                name: "simulation_recommendations");

            migrationBuilder.DropTable(
                name: "projected_transactions");

            migrationBuilder.DropTable(
                name: "monthly_simulations");
        }
    }
}
