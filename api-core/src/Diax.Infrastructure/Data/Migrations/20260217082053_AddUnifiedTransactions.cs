using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_transaction_id",
                table: "imported_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "matched_transaction_id",
                table: "imported_transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "suggested_type",
                table: "imported_transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transaction_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    applicable_to = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    raw_bank_type = table.Column<int>(type: "int", nullable: true),
                    raw_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    payment_method = table.Column<int>(type: "int", nullable: false),
                    is_recurring = table.Column<bool>(type: "bit", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    credit_card_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    credit_card_invoice_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    paid_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    transfer_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    account_transfer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_account_transfers_account_transfer_id",
                        column: x => x.account_transfer_id,
                        principalTable: "account_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transactions_credit_card_invoices_credit_card_invoice_id",
                        column: x => x.credit_card_invoice_id,
                        principalTable: "credit_card_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transactions_credit_cards_credit_card_id",
                        column: x => x.credit_card_id,
                        principalTable: "credit_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transactions_financial_accounts_financial_account_id",
                        column: x => x.financial_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transactions_transaction_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "transaction_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "transaction_categories",
                columns: new[] { "id", "applicable_to", "created_at", "created_by", "is_active", "name", "updated_at", "updated_by", "user_id" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Salário", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Serviço", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Marketing", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Venda", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Comissão", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Reembolso", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Empréstimo", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("10000000-0000-0000-0000-000000000008"), 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Outros", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000001"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Alimentação", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000002"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Transporte", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000003"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Moradia", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000004"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Saúde", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000005"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Educação", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000006"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Lazer", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000007"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Vestuário", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000008"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Serviços", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000009"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Impostos", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000010"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Investimentos", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000011"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Marketing", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000012"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Equipamentos", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000013"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Fornecedores", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000014"), 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Não Categorizado", null, null, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("20000000-0000-0000-0000-000000000015"), 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Outros", null, null, new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_created_transaction_id",
                table: "imported_transactions",
                column: "created_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_matched_transaction_id",
                table: "imported_transactions",
                column: "matched_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_categories_user_id",
                table: "transaction_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_account_transfer_id",
                table: "transactions",
                column: "account_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_category_id",
                table: "transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_credit_card_id",
                table: "transactions",
                column: "credit_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_credit_card_invoice_id",
                table: "transactions",
                column: "credit_card_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_financial_account_id",
                table: "transactions",
                column: "financial_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transfer_group",
                table: "transactions",
                column: "transfer_group_id",
                filter: "[transfer_group_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_account",
                table: "transactions",
                columns: new[] { "user_id", "financial_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_category",
                table: "transactions",
                columns: new[] { "user_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_date",
                table: "transactions",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_type_date",
                table: "transactions",
                columns: new[] { "user_id", "type", "date" });

            migrationBuilder.AddForeignKey(
                name: "FK_imported_transactions_transactions_created_transaction_id",
                table: "imported_transactions",
                column: "created_transaction_id",
                principalTable: "transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_imported_transactions_transactions_matched_transaction_id",
                table: "imported_transactions",
                column: "matched_transaction_id",
                principalTable: "transactions",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_imported_transactions_transactions_created_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_imported_transactions_transactions_matched_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "transaction_categories");

            migrationBuilder.DropIndex(
                name: "IX_imported_transactions_created_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropIndex(
                name: "IX_imported_transactions_matched_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropColumn(
                name: "created_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropColumn(
                name: "matched_transaction_id",
                table: "imported_transactions");

            migrationBuilder.DropColumn(
                name: "suggested_type",
                table: "imported_transactions");
        }
    }
}
