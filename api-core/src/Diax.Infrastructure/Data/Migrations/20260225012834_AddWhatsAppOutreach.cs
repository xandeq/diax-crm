using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppOutreach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "daily_whats_app_limit",
                table: "outreach_configs",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_cold_template",
                table: "outreach_configs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "whats_app_cooldown_days",
                table: "outreach_configs",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_follow_up_template",
                table: "outreach_configs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_hot_template",
                table: "outreach_configs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "whats_app_send_enabled",
                table: "outreach_configs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_warm_template",
                table: "outreach_configs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_whats_app_sent_at",
                table: "customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "whats_app_opt_out",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "whats_app_sent_count",
                table: "customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Segment_Status_WhatsAppOptOut",
                table: "customers",
                columns: new[] { "segment", "status", "whats_app_opt_out" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Segment_Status_WhatsAppOptOut",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "daily_whats_app_limit",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_cold_template",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_cooldown_days",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_follow_up_template",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_hot_template",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_send_enabled",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "whats_app_warm_template",
                table: "outreach_configs");

            migrationBuilder.DropColumn(
                name: "last_whats_app_sent_at",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "whats_app_opt_out",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "whats_app_sent_count",
                table: "customers");
        }
    }
}
