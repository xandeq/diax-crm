using System;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Pipeline de vendas: valor estimado do negócio + data prevista de fechamento.
    /// Migration à mão (dotnet-ef bloqueado pelo Smart App Control nesta máquina) —
    /// atributos inline + snapshot atualizado manualmente.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260703090000_AddDealFieldsToCustomers")]
    public partial class AddDealFieldsToCustomers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "estimated_value",
                table: "customers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expected_close_date",
                table: "customers",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "estimated_value", table: "customers");
            migrationBuilder.DropColumn(name: "expected_close_date", table: "customers");
        }
    }
}
