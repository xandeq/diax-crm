using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardOrganizationAndExpenseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_credit_card_invoices_credit_cards_credit_card_id",
                table: "credit_card_invoices");

            migrationBuilder.RenameColumn(
                name: "credit_card_id",
                table: "credit_card_invoices",
                newName: "credit_card_group_id");

            migrationBuilder.RenameIndex(
                name: "IX_CreditCardInvoices_Card_Period",
                table: "credit_card_invoices",
                newName: "IX_CreditCardInvoices_Group_Period");

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_date",
                table: "expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "brand",
                table: "credit_cards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "card_kind",
                table: "credit_cards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "credit_card_group_id",
                table: "credit_cards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "credit_cards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "credit_card_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    bank = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    closing_day = table.Column<int>(type: "int", nullable: false),
                    due_day = table.Column<int>(type: "int", nullable: false),
                    shared_limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_groups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_cards_credit_card_group_id",
                table: "credit_cards",
                column: "credit_card_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardGroups_IsActive",
                table: "credit_card_groups",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardGroups_Name",
                table: "credit_card_groups",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "FK_credit_card_invoices_credit_card_groups_credit_card_group_id",
                table: "credit_card_invoices",
                column: "credit_card_group_id",
                principalTable: "credit_card_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_credit_cards_credit_card_groups_credit_card_group_id",
                table: "credit_cards",
                column: "credit_card_group_id",
                principalTable: "credit_card_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_credit_card_invoices_credit_card_groups_credit_card_group_id",
                table: "credit_card_invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_credit_cards_credit_card_groups_credit_card_group_id",
                table: "credit_cards");

            migrationBuilder.DropTable(
                name: "credit_card_groups");

            migrationBuilder.DropIndex(
                name: "IX_credit_cards_credit_card_group_id",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "paid_date",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "status",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "brand",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "card_kind",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "credit_card_group_id",
                table: "credit_cards");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "credit_cards");

            migrationBuilder.RenameColumn(
                name: "credit_card_group_id",
                table: "credit_card_invoices",
                newName: "credit_card_id");

            migrationBuilder.RenameIndex(
                name: "IX_CreditCardInvoices_Group_Period",
                table: "credit_card_invoices",
                newName: "IX_CreditCardInvoices_Card_Period");

            migrationBuilder.AddForeignKey(
                name: "FK_credit_card_invoices_credit_cards_credit_card_id",
                table: "credit_card_invoices",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
