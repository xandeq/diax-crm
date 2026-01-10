using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCashFlowTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create a default "Unassigned" account for existing records with null financial_account_id
            var defaultAccountId = Guid.NewGuid();
            migrationBuilder.Sql($@"
                INSERT INTO financial_accounts (id, name, account_type, initial_balance, balance, is_active, created_at)
                VALUES (
                    '{defaultAccountId}',
                    'Contas Não Atribuídas (Migração)',
                    'Others',
                    0.00,
                    0.00,
                    0,
                    GETDATE()
                )
            ");

            // Step 2: Update existing income records with null financial_account_id to use the default account
            migrationBuilder.Sql($@"
                UPDATE incomes
                SET financial_account_id = '{defaultAccountId}'
                WHERE financial_account_id IS NULL
            ");

            // Step 3: Update the default account balance to match the sum of incomes
            migrationBuilder.Sql($@"
                UPDATE financial_accounts
                SET balance = (
                    SELECT ISNULL(SUM(amount), 0)
                    FROM incomes
                    WHERE financial_account_id = '{defaultAccountId}'
                )
                WHERE id = '{defaultAccountId}'
            ");

            // Step 4: Make the column NOT NULL
            migrationBuilder.DropForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes");

            migrationBuilder.AlterColumn<Guid>(
                name: "financial_account_id",
                table: "incomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 5: Create account_transfers table
            migrationBuilder.CreateTable(
                name: "account_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    from_financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    to_financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_transfers", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_transfers_financial_accounts_from_financial_account_id",
                        column: x => x.from_financial_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_transfers_financial_accounts_to_financial_account_id",
                        column: x => x.to_financial_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_transfers_from_financial_account_id",
                table: "account_transfers",
                column: "from_financial_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_transfers_to_financial_account_id",
                table: "account_transfers",
                column: "to_financial_account_id");

            // Step 6: Re-add foreign key with Restrict behavior
            migrationBuilder.AddForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes",
                column: "financial_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes");

            migrationBuilder.DropTable(
                name: "account_transfers");

            migrationBuilder.AlterColumn<Guid>(
                name: "financial_account_id",
                table: "incomes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_financial_accounts_financial_account_id",
                table: "incomes",
                column: "financial_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
