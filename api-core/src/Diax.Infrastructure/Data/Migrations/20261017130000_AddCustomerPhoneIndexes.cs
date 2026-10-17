using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20261017130000_AddCustomerPhoneIndexes")]
    public partial class AddCustomerPhoneIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "customers",
                column: "phone",
                filter: "[phone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_WhatsApp",
                table: "customers",
                column: "whats_app",
                filter: "[whats_app] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Phone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_WhatsApp",
                table: "customers");
        }
    }
}
