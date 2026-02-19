using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCampaignStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "campaign_id",
                table: "email_queue_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    body_html = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    source_snippet_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    total_recipients = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    sent_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    failed_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    open_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_campaigns", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_CampaignId",
                table: "email_queue_items",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaigns_status",
                table: "email_campaigns",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaigns_user_id_created_at",
                table: "email_campaigns",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_email_queue_items_email_campaigns_campaign_id",
                table: "email_queue_items",
                column: "campaign_id",
                principalTable: "email_campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_queue_items_email_campaigns_campaign_id",
                table: "email_queue_items");

            migrationBuilder.DropTable(
                name: "email_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueueItem_CampaignId",
                table: "email_queue_items");

            migrationBuilder.DropColumn(
                name: "campaign_id",
                table: "email_queue_items");
        }
    }
}
