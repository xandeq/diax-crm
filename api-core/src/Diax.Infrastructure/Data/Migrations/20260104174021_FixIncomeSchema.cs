using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixIncomeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_date",
                table: "expenses");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "incomes",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "incomes",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "expenses",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "expenses",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "is_paid",
                table: "expenses",
                newName: "is_recurring");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "expenses",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "expenses",
                newName: "category");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "incomes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_recurring",
                table: "incomes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "payment_method",
                table: "incomes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "last_four_digits",
                table: "credit_cards",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "is_recurring",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "last_four_digits",
                table: "credit_cards");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "incomes",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "incomes",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "is_recurring",
                table: "expenses",
                newName: "is_paid");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "expenses",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "expenses",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "expenses",
                newName: "details");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "expenses",
                newName: "value");

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_date",
                table: "expenses",
                type: "datetime2",
                nullable: true);
        }
    }
}
