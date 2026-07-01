using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailAuditHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailSendLog_IdempotencyKey",
                table: "email_send_log");

            migrationBuilder.AlterColumn<string>(
                name: "updated_by",
                table: "email_suppressions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "email_suppressions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "email_suppressions",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "domain_pattern",
                table: "email_suppressions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "email_suppressions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "updated_by",
                table: "email_events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                table: "email_events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "metadata",
                table: "email_events",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "email_events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppression_User_DomainPattern",
                table: "email_suppressions",
                columns: new[] { "user_id", "domain_pattern" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppression_User_Email",
                table: "email_suppressions",
                columns: new[] { "user_id", "email" });

            migrationBuilder.CreateIndex(
                name: "UX_EmailSendLog_IdempotencyKey_Active",
                table: "email_send_log",
                column: "idempotency_key",
                unique: true,
                filter: "[idempotency_key] IS NOT NULL AND [status] IN ('InFlight', 'Sent')");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_ProviderMessageId",
                table: "email_queue_items",
                column: "provider_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_Status_Provider_ScheduledAt",
                table: "email_queue_items",
                columns: new[] { "status", "assigned_provider", "scheduled_at" });

            // Duplicatas QUEUED pré-existentes quebrariam a criação do índice único —
            // e são exatamente os itens que causariam envio em dobro. Mantém a mais
            // antiga por (campanha, cliente) e remove as demais. Itens Sent/Failed
            // (histórico) NÃO são tocados.
            migrationBuilder.Sql(@"
;WITH dupes AS (
    SELECT id, ROW_NUMBER() OVER (
        PARTITION BY campaign_id, customer_id
        ORDER BY created_at ASC, id ASC) AS rn
    FROM email_queue_items
    WHERE campaign_id IS NOT NULL AND customer_id IS NOT NULL AND status = 0
)
DELETE FROM email_queue_items WHERE id IN (SELECT id FROM dupes WHERE rn > 1);
");

            migrationBuilder.CreateIndex(
                name: "UX_EmailQueueItem_Campaign_Customer",
                table: "email_queue_items",
                columns: new[] { "campaign_id", "customer_id" },
                unique: true,
                filter: "[campaign_id] IS NOT NULL AND [customer_id] IS NOT NULL AND [status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvent_Campaign_EventType",
                table: "email_events",
                columns: new[] { "campaign_id", "event_type" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvent_OccurredAt",
                table: "email_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "UX_EmailEvent_QueueItem_EventType",
                table: "email_events",
                columns: new[] { "queue_item_id", "event_type" },
                unique: true,
                filter: "[queue_item_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailSuppression_User_DomainPattern",
                table: "email_suppressions");

            migrationBuilder.DropIndex(
                name: "IX_EmailSuppression_User_Email",
                table: "email_suppressions");

            migrationBuilder.DropIndex(
                name: "UX_EmailSendLog_IdempotencyKey_Active",
                table: "email_send_log");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueueItem_ProviderMessageId",
                table: "email_queue_items");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueueItem_Status_Provider_ScheduledAt",
                table: "email_queue_items");

            migrationBuilder.DropIndex(
                name: "UX_EmailQueueItem_Campaign_Customer",
                table: "email_queue_items");

            migrationBuilder.DropIndex(
                name: "IX_EmailEvent_Campaign_EventType",
                table: "email_events");

            migrationBuilder.DropIndex(
                name: "IX_EmailEvent_OccurredAt",
                table: "email_events");

            migrationBuilder.DropIndex(
                name: "UX_EmailEvent_QueueItem_EventType",
                table: "email_events");

            migrationBuilder.AlterColumn<string>(
                name: "updated_by",
                table: "email_suppressions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "email_suppressions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "email_suppressions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "domain_pattern",
                table: "email_suppressions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "email_suppressions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "updated_by",
                table: "email_events",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                table: "email_events",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "metadata",
                table: "email_events",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "email_events",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLog_IdempotencyKey",
                table: "email_send_log",
                column: "idempotency_key");
        }
    }
}
