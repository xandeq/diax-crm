using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20261017120000_AddEmailQueueCustomerIndex")]
    public partial class AddEmailQueueCustomerIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItem_CustomerId_CreatedAt",
                table: "email_queue_items",
                columns: new[] { "customer_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailQueueItem_CustomerId_CreatedAt",
                table: "email_queue_items");
        }
    }
}
