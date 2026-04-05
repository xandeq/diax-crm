using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Patch migration: idempotently adds all columns from the failed
    /// AddStatementAmountToCreditCardInvoice migration (20260405145015).
    /// That migration used raw DDL for the first two statements and regular
    /// AddColumn for the rest; if the transaction was rolled back (or the
    /// history row was inserted before the SQL completed), the columns may
    /// be absent even though EF thinks the migration was applied.
    /// </summary>
    public partial class PatchMissingColumnsFromAddStatementAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // recurring_transactions.details
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'recurring_transactions' AND COLUMN_NAME = 'details'
                )
                BEGIN
                    ALTER TABLE recurring_transactions ADD details nvarchar(256) NULL;
                END");

            // credit_card_invoices.statement_amount
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'credit_card_invoices' AND COLUMN_NAME = 'statement_amount'
                )
                BEGIN
                    ALTER TABLE credit_card_invoices ADD statement_amount decimal(18,2) NULL;
                END");

            // ai_usage_logs.error_category
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_usage_logs' AND COLUMN_NAME = 'error_category'
                )
                BEGIN
                    ALTER TABLE ai_usage_logs ADD error_category nvarchar(256) NULL;
                END");

            // ai_usage_logs.http_status_code
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_usage_logs' AND COLUMN_NAME = 'http_status_code'
                )
                BEGIN
                    ALTER TABLE ai_usage_logs ADD http_status_code int NULL;
                END");

            // ai_models.consecutive_failure_count
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_models' AND COLUMN_NAME = 'consecutive_failure_count'
                )
                BEGIN
                    ALTER TABLE ai_models ADD consecutive_failure_count int NOT NULL DEFAULT 0;
                END");

            // ai_models.last_failure_at
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_models' AND COLUMN_NAME = 'last_failure_at'
                )
                BEGIN
                    ALTER TABLE ai_models ADD last_failure_at datetime2 NULL;
                END");

            // ai_models.last_failure_category
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_models' AND COLUMN_NAME = 'last_failure_category'
                )
                BEGIN
                    ALTER TABLE ai_models ADD last_failure_category nvarchar(256) NULL;
                END");

            // ai_models.last_failure_message
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_models' AND COLUMN_NAME = 'last_failure_message'
                )
                BEGIN
                    ALTER TABLE ai_models ADD last_failure_message nvarchar(256) NULL;
                END");

            // ai_models.last_success_at
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'ai_models' AND COLUMN_NAME = 'last_success_at'
                )
                BEGIN
                    ALTER TABLE ai_models ADD last_success_at datetime2 NULL;
                END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // intentionally left minimal — these columns are part of the live model
        }
    }
}
