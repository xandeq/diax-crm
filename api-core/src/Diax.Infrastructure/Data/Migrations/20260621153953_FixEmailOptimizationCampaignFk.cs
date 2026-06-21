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

            // SQL Server cannot convert bigint -> uniqueidentifier via ALTER COLUMN, so drop and
            // re-add the column. Safe because email_optimizations.email_campaign_id is unused/empty
            // (the EmailOptimization entity is never persisted; only a DbSet exists).
            migrationBuilder.DropColumn(
                name: "email_campaign_id",
                table: "email_optimizations");

            migrationBuilder.AddColumn<Guid>(
                name: "email_campaign_id",
                table: "email_optimizations",
                type: "uniqueidentifier",
                nullable: true);

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

            // Reverse of the Up() drop/add: uniqueidentifier -> bigint is likewise not a valid
            // ALTER COLUMN conversion on SQL Server, so drop and re-add.
            migrationBuilder.DropColumn(
                name: "email_campaign_id",
                table: "email_optimizations");

            migrationBuilder.AddColumn<long>(
                name: "email_campaign_id",
                table: "email_optimizations",
                type: "bigint",
                nullable: true);

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
