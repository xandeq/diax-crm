using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementAmountToCreditCardInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Conditional: only drop if column still exists (idempotent for prod environments)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'projected_transactions' AND COLUMN_NAME = 'details'
                )
                BEGIN
                    ALTER TABLE projected_transactions DROP COLUMN details;
                END");

            // Conditional: only add if column does not exist yet
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'recurring_transactions' AND COLUMN_NAME = 'details'
                )
                BEGIN
                    ALTER TABLE recurring_transactions ADD details nvarchar(256) NULL;
                END");

            migrationBuilder.AddColumn<decimal>(
                name: "statement_amount",
                table: "credit_card_invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_category",
                table: "ai_usage_logs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "http_status_code",
                table: "ai_usage_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "consecutive_failure_count",
                table: "ai_models",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_failure_at",
                table: "ai_models",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_category",
                table: "ai_models",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_message",
                table: "ai_models",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_success_at",
                table: "ai_models",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "details",
                table: "recurring_transactions");

            migrationBuilder.DropColumn(
                name: "statement_amount",
                table: "credit_card_invoices");

            migrationBuilder.DropColumn(
                name: "error_category",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "http_status_code",
                table: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "consecutive_failure_count",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "last_failure_at",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "last_failure_category",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "last_failure_message",
                table: "ai_models");

            migrationBuilder.DropColumn(
                name: "last_success_at",
                table: "ai_models");

            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "projected_transactions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
