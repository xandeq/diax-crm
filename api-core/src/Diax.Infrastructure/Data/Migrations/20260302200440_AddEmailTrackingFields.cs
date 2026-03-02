using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "delivered_at",
                table: "email_queue_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "opened_at",
                table: "email_queue_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "read_count",
                table: "email_queue_items",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "email_queue_items");

            migrationBuilder.DropColumn(
                name: "opened_at",
                table: "email_queue_items");

            migrationBuilder.DropColumn(
                name: "read_count",
                table: "email_queue_items");
        }
    }
}
