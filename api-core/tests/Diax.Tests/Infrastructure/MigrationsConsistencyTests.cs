using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure;

public class MigrationsConsistencyTests
{
    [Fact]
    public void Model_MatchesLatestMigrationSnapshot()
    {
        // Não abre conexão: só compara o modelo do DbContext com o DiaxDbContextModelSnapshot.
        // Se falhar, alguém mudou Configuration/entidade sem migration (ou editou o snapshot errado),
        // e o db.Database.Migrate() do startup quebraria em prod.
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseSqlServer("Server=localhost;Database=diax_model_check;Trusted_Connection=True;")
            .Options;

        using var db = new DiaxDbContext(options);

        Assert.False(db.Database.HasPendingModelChanges());
    }
}
