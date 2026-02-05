using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToFinancialEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "statement_imports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "incomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "income_categories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "imported_transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "financial_accounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "expenses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "expense_categories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "credit_cards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "credit_card_invoices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "credit_card_groups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "account_transfers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                DECLARE @DefaultUserId UNIQUEIDENTIFIER;
                SELECT TOP 1 @DefaultUserId = id FROM admin_users ORDER BY created_at;

                IF @DefaultUserId IS NULL
                    SET @DefaultUserId = '11111111-1111-1111-1111-111111111111';

                UPDATE statement_imports SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE incomes SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE income_categories SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE imported_transactions SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE financial_accounts SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE expenses SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE expense_categories SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE credit_cards SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE credit_card_invoices SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE credit_card_groups SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                UPDATE account_transfers SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000012"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000013"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000014"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "expense_categories",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000015"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "income_categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "user_id",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateIndex(
                name: "IX_statement_imports_user_id",
                table: "statement_imports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incomes_user_id",
                table: "incomes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_income_categories_user_id",
                table: "income_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_user_id",
                table: "imported_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_user_id",
                table: "financial_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_user_id",
                table: "expenses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_categories_user_id",
                table: "expense_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_cards_user_id",
                table: "credit_cards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_invoices_user_id",
                table: "credit_card_invoices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_groups_user_id",
                table: "credit_card_groups",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_transfers_user_id",
                table: "account_transfers",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_statement_imports_user_id",
                table: "statement_imports");

            migrationBuilder.DropIndex(
                name: "IX_incomes_user_id",
                table: "incomes");

            migrationBuilder.DropIndex(
                name: "IX_income_categories_user_id",
                table: "income_categories");

            migrationBuilder.DropIndex(
                name: "IX_imported_transactions_user_id",
                table: "imported_transactions");

            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_user_id",
                table: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_expenses_user_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expense_categories_user_id",
                table: "expense_categories");

            migrationBuilder.DropIndex(
                name: "IX_credit_cards_user_id",
                table: "credit_cards");

            migrationBuilder.DropIndex(
                name: "IX_credit_card_invoices_user_id",
                table: "credit_card_invoices");

            migrationBuilder.DropIndex(
                name: "IX_credit_card_groups_user_id",
                table: "credit_card_groups");

            migrationBuilder.DropIndex(
                name: "IX_account_transfers_user_id",
                table: "account_transfers");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "statement_imports");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "income_categories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "imported_transactions");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "expense_categories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "credit_card_invoices");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "credit_card_groups");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "account_transfers");
        }
    }
}
