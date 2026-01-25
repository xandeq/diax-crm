using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementImportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "statement_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    import_type = table.Column<int>(type: "int", nullable: false),
                    financial_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    credit_card_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    file_content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    total_records = table.Column<int>(type: "int", nullable: false),
                    processed_records = table.Column<int>(type: "int", nullable: false),
                    failed_records = table.Column<int>(type: "int", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_imports", x => x.id);
                    table.ForeignKey(
                        name: "FK_statement_imports_credit_card_groups_credit_card_group_id",
                        column: x => x.credit_card_group_id,
                        principalTable: "credit_card_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_statement_imports_financial_accounts_financial_account_id",
                        column: x => x.financial_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "imported_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    statement_import_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    raw_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    matched_expense_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_expense_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imported_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_imported_transactions_expenses_created_expense_id",
                        column: x => x.created_expense_id,
                        principalTable: "expenses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_imported_transactions_expenses_matched_expense_id",
                        column: x => x.matched_expense_id,
                        principalTable: "expenses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_imported_transactions_statement_imports_statement_import_id",
                        column: x => x.statement_import_id,
                        principalTable: "statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_created_expense_id",
                table: "imported_transactions",
                column: "created_expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_matched_expense_id",
                table: "imported_transactions",
                column: "matched_expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_statement_import_id",
                table: "imported_transactions",
                column: "statement_import_id");

            migrationBuilder.CreateIndex(
                name: "IX_statement_imports_credit_card_group_id",
                table: "statement_imports",
                column: "credit_card_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_statement_imports_financial_account_id",
                table: "statement_imports",
                column: "financial_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imported_transactions");

            migrationBuilder.DropTable(
                name: "statement_imports");
        }
    }
}
