using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatrimonioFipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fipe_brand_code",
                table: "assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fipe_model_code",
                table: "assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fipe_vehicle_type",
                table: "assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fipe_year_code",
                table: "assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fipe_brand_code",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "fipe_model_code",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "fipe_vehicle_type",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "fipe_year_code",
                table: "assets");
        }
    }
}
