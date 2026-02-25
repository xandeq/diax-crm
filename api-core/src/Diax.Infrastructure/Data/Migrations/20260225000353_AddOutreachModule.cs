using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutreachModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "email_opt_out",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "email_sent_count",
                table: "customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_email_sent_at",
                table: "customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lead_score",
                table: "customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "segment",
                table: "customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outreach_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apify_dataset_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    apify_api_token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    import_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    segmentation_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    send_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    daily_email_limit = table.Column<int>(type: "int", nullable: false, defaultValue: 200),
                    email_cooldown_days = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    hot_template_subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    hot_template_body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    warm_template_subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    warm_template_body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    cold_template_subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    cold_template_body = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outreach_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Segment_Status_OptOut",
                table: "customers",
                columns: new[] { "segment", "status", "email_opt_out" });

            migrationBuilder.CreateIndex(
                name: "IX_OutreachConfigs_UserId",
                table: "outreach_configs",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outreach_configs");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Segment_Status_OptOut",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "email_opt_out",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "email_sent_count",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_email_sent_at",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "lead_score",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "segment",
                table: "customers");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
