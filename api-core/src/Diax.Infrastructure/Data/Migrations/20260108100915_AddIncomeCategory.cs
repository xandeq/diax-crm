using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "incomes");

            migrationBuilder.AddColumn<Guid>(
                name: "income_category_id",
                table: "incomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "income_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_income_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incomes_income_category_id",
                table: "incomes",
                column: "income_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes",
                column: "income_category_id",
                principalTable: "income_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incomes_income_categories_income_category_id",
                table: "incomes");

            migrationBuilder.DropTable(
                name: "income_categories");

            migrationBuilder.DropIndex(
                name: "IX_incomes_income_category_id",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "income_category_id",
                table: "incomes");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "incomes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
