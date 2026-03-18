using Diax.Application.AI.QuotaManagement;
using Diax.Infrastructure.Data;
using Diax.Shared;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.AI.QuotaManagement;

/// <summary>
/// Service for enforcing AI provider quota limits.
/// Tracks daily/monthly usage and enforces free-tier constraints.
/// </summary>
public class AiQuotaService : IAiQuotaService
{
    private readonly DiaxDbContext _db;
    private readonly ILogger<AiQuotaService> _logger;

    public AiQuotaService(DiaxDbContext db, ILogger<AiQuotaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<bool>> CanUserGenerateAsync(Guid providerId, int creditsOrGenerations = 1, CancellationToken ct = default)
    {
        var quota = await _db.AiProviderQuotas
            .FirstOrDefaultAsync(q => q.AiProviderId == providerId, ct);

        if (quota == null || !quota.IsEnforced)
            return Result<bool>.Success(true); // No quota defined or not enforced, allow

        // Reset if past reset date
        if (quota.LastResetDate.Date < DateTime.UtcNow.Date)
        {
            quota.ResetDailyQuota();
            _db.AiProviderQuotas.Update(quota);
            await _db.SaveChangesAsync();
        }

        // Check if can record usage
        if (!quota.RecordUsage(creditsOrGenerations))
        {
            var remaining = quota.GetRemainingDaily();
            var resetTime = quota.LastResetDate.AddDays(1);
            var error = new Error(
                "QuotaExceeded",
                $"Daily {quota.QuotaType.ToLower()} limit ({quota.DailyGenerationLimit ?? quota.DailyCreditsLimit}) exceeded. " +
                $"Remaining: {remaining}. Resets at {resetTime:HH:mm} UTC.");
            return Result.Failure<bool>(error);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result> RecordGenerationAsync(Guid providerId, int creditsOrGenerations = 1, CancellationToken ct = default)
    {
        try
        {
            var quota = await _db.AiProviderQuotas
                .FirstOrDefaultAsync(q => q.AiProviderId == providerId, ct);

            if (quota == null || !quota.IsEnforced)
                return Result.Success(); // No quota to record

            // Reset if past reset date
            if (quota.LastResetDate.Date < DateTime.UtcNow.Date)
            {
                quota.ResetDailyQuota();
            }

            // Record the usage
            var recorded = quota.RecordUsage(creditsOrGenerations);
            _db.AiProviderQuotas.Update(quota);
            await _db.SaveChangesAsync(ct);

            if (recorded)
            {
                _logger.LogInformation(
                    "[Quota] Provider {ProviderId}: {Usage}/{Limit} {Type} used",
                    providerId,
                    quota.CurrentDailyUsage,
                    quota.DailyGenerationLimit ?? quota.DailyCreditsLimit,
                    quota.QuotaType);
            }
            else
            {
                _logger.LogWarning(
                    "[Quota] Provider {ProviderId}: Could not record {Credits} {Type}",
                    providerId,
                    creditsOrGenerations,
                    quota.QuotaType);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Quota] Failed to record usage for provider {ProviderId}", providerId);
            return Result.Failure(new Error("QuotaRecordingError", ex.Message));
        }
    }

    public async Task<QuotaStatusDto?> GetQuotaStatusAsync(Guid providerId, CancellationToken ct = default)
    {
        var quota = await _db.AiProviderQuotas
            .Include(q => q.AiProvider)
            .FirstOrDefaultAsync(q => q.AiProviderId == providerId, ct);

        if (quota == null)
            return null;

        // Reset if past reset date
        if (quota.LastResetDate.Date < DateTime.UtcNow.Date)
        {
            quota.ResetDailyQuota();
            _db.AiProviderQuotas.Update(quota);
            await _db.SaveChangesAsync(ct);
        }

        var resetTime = quota.LastResetDate.AddDays(1);
        var timeUntilReset = resetTime - DateTime.UtcNow;

        var limit = quota.DailyGenerationLimit ?? quota.DailyCreditsLimit ?? 0;

        return new QuotaStatusDto
        {
            ProviderId = quota.AiProviderId,
            ProviderName = quota.AiProvider.Name,
            QuotaType = quota.QuotaType,
            DailyLimit = limit,
            CurrentUsage = quota.CurrentDailyUsage,
            Remaining = Math.Max(0, limit - quota.CurrentDailyUsage),
            PercentageUsed = quota.GetUsagePercentage(),
            ResetAt = resetTime,
            TimeUntilReset = timeUntilReset,
            IsExhausted = quota.IsQuotaExhausted()
        };
    }

    public async Task ResetDailyQuotasAsync(CancellationToken ct = default)
    {
        try
        {
            var quotas = await _db.AiProviderQuotas
                .Where(q => q.ResetFrequency == "Daily" && q.LastResetDate.Date < DateTime.UtcNow.Date)
                .ToListAsync(ct);

            foreach (var quota in quotas)
            {
                quota.ResetDailyQuota();
            }

            if (quotas.Count > 0)
            {
                _db.AiProviderQuotas.UpdateRange(quotas);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("[Quota] Reset {Count} daily quotas", quotas.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Quota] Failed to reset daily quotas");
        }
    }
}
