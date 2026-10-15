using Diax.Domain.SurfAlerts;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class SurfAlertRepository : ISurfAlertRepository
{
    private readonly DiaxDbContext _db;

    public SurfAlertRepository(DiaxDbContext db) => _db = db;

    public async Task AddAsync(SurfAlert alert, CancellationToken ct = default)
        => await _db.SurfAlerts.AddAsync(alert, ct);

    public async Task<SurfAlert?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => await _db.SurfAlerts.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);

    public async Task<IReadOnlyList<SurfAlert>> GetRecentAsync(int limit, CancellationToken ct = default)
        => await _db.SurfAlerts.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
