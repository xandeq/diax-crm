using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardIdToStatementImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "credit_card_id",
                table: "statement_imports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_statement_imports_credit_card_id",
                table: "statement_imports",
                column: "credit_card_id");

            migrationBuilder.AddForeignKey(
                name: "FK_statement_imports_credit_cards_credit_card_id",
                table: "statement_imports",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_statement_imports_credit_cards_credit_card_id",
                table: "statement_imports");

            migrationBuilder.DropIndex(
                name: "IX_statement_imports_credit_card_id",
                table: "statement_imports");

            migrationBuilder.DropColumn(
                name: "credit_card_id",
                table: "statement_imports");
        }
    }
}
