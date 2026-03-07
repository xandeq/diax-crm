using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadSanitizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "email_type",
                table: "customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_suspicious_domain",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_eligible_for_campaigns",
                table: "customers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "quality",
                table: "customers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_type",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "has_suspicious_domain",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_eligible_for_campaigns",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "quality",
                table: "customers");
        }
    }
}
