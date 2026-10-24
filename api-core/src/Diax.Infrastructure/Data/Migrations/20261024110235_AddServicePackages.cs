using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_follow_up_stage",
                table: "proposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "origin",
                table: "proposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "package_id",
                table: "proposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    short_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    scope = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_days = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_packages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proposals_status_sent_at",
                table: "proposals",
                columns: new[] { "status", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_packages_user_id",
                table: "service_packages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_packages_user_id_is_active_sort_order",
                table: "service_packages",
                columns: new[] { "user_id", "is_active", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_packages");

            migrationBuilder.DropIndex(
                name: "IX_proposals_status_sent_at",
                table: "proposals");

            migrationBuilder.DropColumn(
                name: "last_follow_up_stage",
                table: "proposals");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "proposals");

            migrationBuilder.DropColumn(
                name: "package_id",
                table: "proposals");
        }
    }
}
