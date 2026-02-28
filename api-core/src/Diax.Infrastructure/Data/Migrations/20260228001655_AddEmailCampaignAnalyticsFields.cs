using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCampaignAnalyticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bounce_count",
                table: "email_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "click_count",
                table: "email_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "delivered_count",
                table: "email_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "unsubscribe_count",
                table: "email_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bounce_count",
                table: "email_campaigns");

            migrationBuilder.DropColumn(
                name: "click_count",
                table: "email_campaigns");

            migrationBuilder.DropColumn(
                name: "delivered_count",
                table: "email_campaigns");

            migrationBuilder.DropColumn(
                name: "unsubscribe_count",
                table: "email_campaigns");
        }
    }
}
