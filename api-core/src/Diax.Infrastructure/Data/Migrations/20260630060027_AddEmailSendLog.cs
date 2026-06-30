using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSendLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_send_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    to_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    subject_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    body_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    attempt_no = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    latency_ms = table.Column<int>(type: "int", nullable: false),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    from_domain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    allow_unaligned = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_send_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLog_CreatedAt",
                table: "email_send_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLog_Idem_CreatedAt",
                table: "email_send_log",
                columns: new[] { "idempotency_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLog_IdempotencyKey",
                table: "email_send_log",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLog_RequestId",
                table: "email_send_log",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_send_log");
        }
    }
}
