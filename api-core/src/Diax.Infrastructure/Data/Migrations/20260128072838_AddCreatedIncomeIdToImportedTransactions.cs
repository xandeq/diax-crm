using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedIncomeIdToImportedTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_income_id",
                table: "imported_transactions",
                type: "uniqueidentifier",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_imported_transactions_created_income_id",
                table: "imported_transactions",
                column: "created_income_id");

            migrationBuilder.AddForeignKey(
                name: "FK_imported_transactions_incomes_created_income_id",
                table: "imported_transactions",
                column: "created_income_id",
                principalTable: "incomes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_imported_transactions_incomes_created_income_id",
                table: "imported_transactions");

            migrationBuilder.DropIndex(
                name: "IX_imported_transactions_created_income_id",
                table: "imported_transactions");

            migrationBuilder.DropColumn(
                name: "created_income_id",
                table: "imported_transactions");

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
        }
    }
}
