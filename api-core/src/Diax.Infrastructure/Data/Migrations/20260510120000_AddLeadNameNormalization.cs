using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260510120000_AddLeadNameNormalization")]
    public partial class AddLeadNameNormalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_by",
                table: "customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "normalization_score",
                table: "customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "normalized_at",
                table: "customers",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "normalization_source",
                table: "customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedName",
                table: "customers",
                column: "normalized_name",
                filter: "[normalized_name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedName",
                table: "customers");

            migrationBuilder.DropColumn(name: "normalized_name",     table: "customers");
            migrationBuilder.DropColumn(name: "normalized_by",       table: "customers");
            migrationBuilder.DropColumn(name: "normalization_score", table: "customers");
            migrationBuilder.DropColumn(name: "normalized_at",       table: "customers");
            migrationBuilder.DropColumn(name: "normalization_source", table: "customers");
        }
    }
}
