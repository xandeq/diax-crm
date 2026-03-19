using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiModelRepository : Repository<AiModel>, IAiModelRepository
{
    public AiModelRepository(DiaxDbContext context) : base(context) { }

    public async Task<IEnumerable<AiModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ProviderId == providerId)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AiModel>> GetEnabledByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ProviderId == providerId && x.IsEnabled)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiModel?> GetByProviderAndModelKeyAsync(Guid providerId, string modelKey, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ModelKey == modelKey, cancellationToken);
    }

    /// <summary>
    /// Persists only the failure tracking columns for the given model using an efficient UPDATE.
    /// Avoids updating unrelated fields and prevents accidental overwrite of IsEnabled etc.
    /// </summary>
    public async Task UpdateFailureTrackingAsync(AiModel model, CancellationToken cancellationToken = default)
    {
        await Context.Database.ExecuteSqlRawAsync(
            @"UPDATE ai_models
              SET consecutive_failure_count = {0},
                  last_failure_at          = {1},
                  last_success_at          = {2},
                  last_failure_category    = {3},
                  last_failure_message     = {4}
              WHERE id = {5}",
            model.ConsecutiveFailureCount,
            (object?)model.LastFailureAt ?? DBNull.Value,
            (object?)model.LastSuccessAt ?? DBNull.Value,
            (object?)model.LastFailureCategory ?? DBNull.Value,
            (object?)model.LastFailureMessage ?? DBNull.Value,
            model.Id,
            cancellationToken);
    }
}
