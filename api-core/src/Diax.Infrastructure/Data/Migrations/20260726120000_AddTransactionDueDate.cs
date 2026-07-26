using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Vencimento cruzado: due_date nullable em transactions — vencimento real quando difere
    /// do mês de competência (date). Migration à mão (SAC bloqueia dotnet-ef) e IDEMPOTENTE:
    /// a coluna pode já ter sido criada manualmente em produção antes do deploy.
    /// </summary>
    [DbContext(typeof(DiaxDbContext))]
    [Migration("20260726120000_AddTransactionDueDate")]
    public partial class AddTransactionDueDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('transactions', 'due_date') IS NULL " +
                "ALTER TABLE transactions ADD due_date datetime2 NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('transactions', 'due_date') IS NOT NULL " +
                "ALTER TABLE transactions DROP COLUMN due_date;");
        }
    }
}
