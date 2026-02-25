using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupPlaceholderEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpa emails fictícios (sem-email-{guid}@placeholder.local) setando para NULL.
            // O campo email já é nullable desde a migration UpdateCustomerPhoneColumnsSize.
            migrationBuilder.Sql(
                "UPDATE customers SET email = NULL WHERE email LIKE 'sem-email-%@placeholder.local'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: emails fictícios não podem ser restaurados.
        }
    }
}
