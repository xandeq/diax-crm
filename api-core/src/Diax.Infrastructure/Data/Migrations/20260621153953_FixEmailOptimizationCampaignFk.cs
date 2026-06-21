using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailOptimizationCampaignFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_optimizations_email_campaigns_email_campaign_id1",
                table: "email_optimizations");

            migrationBuilder.DropIndex(
                name: "IX_email_optimizations_email_campaign_id1",
                table: "email_optimizations");

            migrationBuilder.DropColumn(
                name: "email_campaign_id1",
                table: "email_optimizations");

            migrationBuilder.AlterColumn<Guid>(
                name: "email_campaign_id",
                table: "email_optimizations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_optimizations_email_campaign_id",
                table: "email_optimizations",
                column: "email_campaign_id");

            migrationBuilder.AddForeignKey(
                name: "FK_email_optimizations_email_campaigns_email_campaign_id",
                table: "email_optimizations",
                column: "email_campaign_id",
                principalTable: "email_campaigns",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_optimizations_email_campaigns_email_campaign_id",
                table: "email_optimizations");

            migrationBuilder.DropIndex(
                name: "IX_email_optimizations_email_campaign_id",
                table: "email_optimizations");

            migrationBuilder.AlterColumn<long>(
                name: "email_campaign_id",
                table: "email_optimizations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "email_campaign_id1",
                table: "email_optimizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_optimizations_email_campaign_id1",
                table: "email_optimizations",
                column: "email_campaign_id1");

            migrationBuilder.AddForeignKey(
                name: "FK_email_optimizations_email_campaigns_email_campaign_id1",
                table: "email_optimizations",
                column: "email_campaign_id1",
                principalTable: "email_campaigns",
                principalColumn: "id");
        }
    }
}
