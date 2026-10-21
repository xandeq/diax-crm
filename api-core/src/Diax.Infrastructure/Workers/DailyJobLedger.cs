using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Workers;

/// <summary>"Já rodei hoje?" persistido no banco (sobrevive a recycle/deploy).</summary>
public static class DailyJobLedger
{
    public static Task<bool> HasRunAsync(DiaxDbContext db, string job, DateOnly day, CancellationToken ct)
        => db.JobRuns.AsNoTracking().AnyAsync(r => r.Job == job && r.RunDate == day, ct);

    public static async Task MarkRunAsync(DiaxDbContext db, string job, DateOnly day, CancellationToken ct)
    {
        if (await HasRunAsync(db, job, day, ct)) return;
        db.JobRuns.Add(new JobRun(job, day));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Outra instância marcou no meio do caminho (PK job+dia) — já está registrado.
        }
    }
}
