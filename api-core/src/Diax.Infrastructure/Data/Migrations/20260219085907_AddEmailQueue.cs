using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_queue_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipient_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    recipient_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    html_body = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    attachments_json = table.Column<string>(type: "nvarchar(max)", maxLength: 2000000, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    scheduled_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    processing_started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    attempt_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_queue_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_SentAt",
                table: "email_queue_items",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_Status_ScheduledAt",
                table: "email_queue_items",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_UserId_CreatedAt",
                table: "email_queue_items",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_queue_items");
        }
    }
}
