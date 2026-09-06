using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadQualitySignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "customers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "website_kind",
                table: "customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "duplicate_rejected_count",
                table: "customer_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "geo_rejected_count",
                table: "customer_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "low_quality_email_rejected_count",
                table: "customer_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "no_mx_rejected_count",
                table: "customer_imports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "mx_cache_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    domain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    result_code = table.Column<int>(type: "int", nullable: false),
                    checked_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mx_cache_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ExternalId",
                table: "customers",
                column: "external_id",
                unique: true,
                filter: "[external_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MxCacheEntries_CheckedAt",
                table: "mx_cache_entries",
                column: "checked_at");

            migrationBuilder.CreateIndex(
                name: "IX_MxCacheEntries_Domain",
                table: "mx_cache_entries",
                column: "domain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mx_cache_entries");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ExternalId",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "website_kind",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "duplicate_rejected_count",
                table: "customer_imports");

            migrationBuilder.DropColumn(
                name: "geo_rejected_count",
                table: "customer_imports");

            migrationBuilder.DropColumn(
                name: "low_quality_email_rejected_count",
                table: "customer_imports");

            migrationBuilder.DropColumn(
                name: "no_mx_rejected_count",
                table: "customer_imports");
        }
    }
}
