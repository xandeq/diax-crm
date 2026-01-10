using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryAndUpdateExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "expenses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "expenses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "expense_category_id",
                table: "expenses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_categories", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "expense_categories",
                columns: new[] { "id", "created_at", "created_by", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Alimentação", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Transporte", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Moradia", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Saúde", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Educação", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Lazer", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Vestuário", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Serviços", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Impostos", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Investimentos", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Marketing", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000012"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Equipamentos", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000013"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Fornecedores", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000014"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Não Categorizado", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000015"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Outros", null, null }
                });

            // Data migration: Map existing Category strings to ExpenseCategory FK
            migrationBuilder.Sql(@"
                -- Update expenses with matching category names
                UPDATE e
                SET e.expense_category_id = ec.id
                FROM expenses e
                INNER JOIN expense_categories ec ON e.category = ec.name
                WHERE e.category IS NOT NULL AND e.category != '';

                -- Set default category for null/empty categories
                UPDATE expenses
                SET expense_category_id = '20000000-0000-0000-0000-000000000014'
                WHERE category IS NULL OR category = '';

                -- Create categories for any unique category strings not in seed data
                INSERT INTO expense_categories (id, name, is_active, created_at)
                SELECT 
                    NEWID(),
                    category,
                    1,
                    GETUTCDATE()
                FROM (
                    SELECT DISTINCT category
                    FROM expenses
                    WHERE category IS NOT NULL 
                      AND category != ''
                      AND NOT EXISTS (SELECT 1 FROM expense_categories WHERE name = expenses.category)
                ) AS unique_categories;

                -- Map newly created categories
                UPDATE e
                SET e.expense_category_id = ec.id
                FROM expenses e
                INNER JOIN expense_categories ec ON e.category = ec.name
                WHERE e.expense_category_id = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_expense_category_id",
                table: "expenses",
                column: "expense_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_expense_categories_expense_category_id",
                table: "expenses",
                column: "expense_category_id",
                principalTable: "expense_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_expenses_expense_categories_expense_category_id",
                table: "expenses");

            migrationBuilder.DropTable(
                name: "expense_categories");

            migrationBuilder.DropIndex(
                name: "IX_expenses_expense_category_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "expense_category_id",
                table: "expenses");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "expenses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "expenses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id");
        }
    }
}
