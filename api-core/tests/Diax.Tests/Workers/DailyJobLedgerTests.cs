using Diax.Infrastructure.Data;
using Diax.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Workers;

// 21/10: "já rodei hoje" vivia só em memória → cada deploy depois do horário reenviava o briefing.
public class DailyJobLedgerTests
{
    private static DbContextOptions<DiaxDbContext> Options() =>
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase("ledger-" + Guid.NewGuid()).Options;

    [Fact]
    public async Task Mark_SurvivesNewContext_LikeARecycle()
    {
        var options = Options();
        var today = new DateOnly(2026, 10, 21);

        await using (var before = new DiaxDbContext(options))
        {
            Assert.False(await DailyJobLedger.HasRunAsync(before, "commercial-briefing", today, default));
            await DailyJobLedger.MarkRunAsync(before, "commercial-briefing", today, default);
        }

        await using var afterRecycle = new DiaxDbContext(options); // processo novo, memória zerada
        Assert.True(await DailyJobLedger.HasRunAsync(afterRecycle, "commercial-briefing", today, default));
        Assert.False(await DailyJobLedger.HasRunAsync(afterRecycle, "commercial-briefing", today.AddDays(1), default));
        Assert.False(await DailyJobLedger.HasRunAsync(afterRecycle, "extractor-pull", today, default));
    }

    [Fact]
    public async Task Mark_Twice_IsIdempotent()
    {
        await using var db = new DiaxDbContext(Options());
        var day = new DateOnly(2026, 10, 21);

        await DailyJobLedger.MarkRunAsync(db, "extractor-pull", day, default);
        await DailyJobLedger.MarkRunAsync(db, "extractor-pull", day, default);

        Assert.Equal(1, await db.JobRuns.CountAsync());
    }
}
