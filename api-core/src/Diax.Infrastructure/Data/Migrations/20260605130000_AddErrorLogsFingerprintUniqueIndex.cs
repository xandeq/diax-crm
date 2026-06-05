using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogsFingerprintUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índice único filtrado: impede duplicatas de fingerprint+appName em logs abertos.
            // A unicidade só se aplica a registros com fingerprint não-nulo e não resolvidos.
            // Quando o log é resolvido (is_resolved=1), sai do índice filtrado → próxima ocorrência cria nova linha.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_error_logs_fingerprint_unique_open]
                ON [error_logs] ([fingerprint], [app_name])
                WHERE [fingerprint] IS NOT NULL AND [is_resolved] = 0;
            ");

            migrationBuilder.InsertData(
                table: "__EFMigrationsHistory",
                columns: new[] { "MigrationId", "ProductVersion" },
                values: new object[] { "20260605130000_AddErrorLogsFingerprintUniqueIndex", "8.0.11" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_error_logs_fingerprint_unique_open] ON [error_logs];");
        }
    }
}
