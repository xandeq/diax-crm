using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountsAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "financial_account_id",
                table: "incomes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "credit_card_invoice_id",
                table: "expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "financial_account_id",
                table: "expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    initial_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reference_month = table.Column<int>(type: "int", nullable: false),
                    reference_year = table.Column<int>(type: "int", nullable: false),
                    closing_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_paid = table.Column<bool>(type: "bit", nullable: false),
                    payment_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    paid_from_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_card_invoices_credit_cards_credit_card_id",
                        column: x => x.credit_card_id,
                        principalTable: "credit_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_card_invoices_financial_accounts_paid_from_account_id",
                        column: x => x.paid_from_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incomes_financial_account_id",
                table: "incomes",
                column: "financial_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_credit_card_invoice_id",
                table: "expenses",
                column: "credit_card_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_financial_account_id",
                table: "expenses",
                column: "financial_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_invoices_paid_from_account_id",
                table: "credit_card_invoices",
                column: "paid_from_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInvoices_Card_Period",
                table: "credit_card_invoices",
                columns: new[] { "credit_card_id", "reference_month", "reference_year" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_credit_card_invoices_credit_card_invoice_id",
                table: "expenses",
                column: "credit_card_invoice_id",
                principalTable: "credit_card_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_financial_accounts_financial_account_id",
                table: "expenses",
                column: "financial_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes",
                column: "financial_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_credit_card_invoices_credit_card_invoice_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_expenses_financial_accounts_financial_account_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes");

            migrationBuilder.DropTable(
                name: "credit_card_invoices");

            migrationBuilder.DropTable(
                name: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_incomes_financial_account_id",
                table: "incomes");

            migrationBuilder.DropIndex(
                name: "IX_expenses_credit_card_invoice_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expenses_financial_account_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "financial_account_id",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "credit_card_invoice_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "financial_account_id",
                table: "expenses");
        }
    }
}
