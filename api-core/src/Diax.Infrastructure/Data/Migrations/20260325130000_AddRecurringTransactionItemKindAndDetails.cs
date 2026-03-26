using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    public partial class AddRecurringTransactionItemKindAndDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "transactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_subscription",
                table: "transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "recurring_transaction_id",
                table: "transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "recurring_transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "item_kind",
                table: "recurring_transactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_recurring_transaction_id",
                table: "transactions",
                column: "recurring_transaction_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_recurring_transactions_recurring_transaction_id",
                table: "transactions",
                column: "recurring_transaction_id",
                principalTable: "recurring_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_recurring_transactions_recurring_transaction_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_recurring_transaction_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "details",
                table: "recurring_transactions");

            migrationBuilder.DropColumn(
                name: "item_kind",
                table: "recurring_transactions");

            migrationBuilder.DropColumn(
                name: "details",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "is_subscription",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "recurring_transaction_id",
                table: "transactions");
        }
    }
}
