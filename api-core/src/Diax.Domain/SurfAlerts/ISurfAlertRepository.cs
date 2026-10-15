namespace Diax.Domain.SurfAlerts;

public interface ISurfAlertRepository
{
    Task AddAsync(SurfAlert alert, CancellationToken ct = default);
    Task<SurfAlert?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<SurfAlert>> GetRecentAsync(int limit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
