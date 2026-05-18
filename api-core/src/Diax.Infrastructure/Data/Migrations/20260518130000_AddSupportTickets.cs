using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    priority = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    category = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    customer_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_user_id",
                table: "support_tickets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_customer_id",
                table: "support_tickets",
                column: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "support_tickets");
        }
    }
}
