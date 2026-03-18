using Diax.Domain.Common;

namespace Diax.Domain.AI;

/// <summary>
/// Tracks quota limits and usage for AI providers (free-tier enforcement).
/// Supports multiple quota types: daily generations, daily credits, monthly limits, cost limits.
/// </summary>
public class AiProviderQuota : AuditableEntity
{
    public Guid AiProviderId { get; private set; }

    // Quota Limits
    public int? DailyGenerationLimit { get; private set; }
    public int? DailyCreditsLimit { get; private set; }
    public int? MonthlyGenerationLimit { get; private set; }
    public decimal? DailyCostLimit { get; private set; }

    // Tracking Current Usage
    public DateTime LastResetDate { get; private set; }
    public int CurrentDailyUsage { get; private set; }
    public decimal CurrentDailyCost { get; private set; }

    // Metadata
    public string QuotaType { get; private set; } // "Generations", "Credits", "Minutes", "Cost"
    public string ResetFrequency { get; private set; } // "Daily", "Monthly"
    public bool IsEnforced { get; private set; }

    // Navigation property
    public AiProvider AiProvider { get; private set; } = null!;

    private AiProviderQuota() { } // EF Core

    public AiProviderQuota(
        Guid aiProviderId,
        string quotaType,
        string resetFrequency,
        int? dailyGenerationLimit = null,
        int? dailyCreditsLimit = null,
        int? monthlyGenerationLimit = null,
        decimal? dailyCostLimit = null,
        bool isEnforced = true)
    {
        AiProviderId = aiProviderId;
        QuotaType = quotaType;
        ResetFrequency = resetFrequency;
        DailyGenerationLimit = dailyGenerationLimit;
        DailyCreditsLimit = dailyCreditsLimit;
        MonthlyGenerationLimit = monthlyGenerationLimit;
        DailyCostLimit = dailyCostLimit;
        IsEnforced = isEnforced;
        LastResetDate = DateTime.UtcNow;
        CurrentDailyUsage = 0;
        CurrentDailyCost = 0;
    }

    /// <summary>
    /// Records usage against the quota. Returns true if within limits.
    /// </summary>
    public bool RecordUsage(int creditsOrGenerationsUsed = 1, decimal costIncurred = 0)
    {
        // Check daily generation limit
        if (DailyGenerationLimit.HasValue)
        {
            if (CurrentDailyUsage >= DailyGenerationLimit.Value)
                return false;
        }

        // Check daily credits limit
        if (DailyCreditsLimit.HasValue)
        {
            if (CurrentDailyUsage + creditsOrGenerationsUsed > DailyCreditsLimit.Value)
                return false;
        }

        // Check daily cost limit
        if (DailyCostLimit.HasValue)
        {
            if (CurrentDailyCost + costIncurred > DailyCostLimit.Value)
                return false;
        }

        // Record the usage
        CurrentDailyUsage += creditsOrGenerationsUsed;
        CurrentDailyCost += costIncurred;
        return true;
    }

    /// <summary>
    /// Resets daily quota counters. Called automatically at midnight UTC.
    /// </summary>
    public void ResetDailyQuota()
    {
        CurrentDailyUsage = 0;
        CurrentDailyCost = 0;
        LastResetDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates remaining quota for the day.
    /// </summary>
    public int GetRemainingDaily()
    {
        var limit = DailyGenerationLimit ?? DailyCreditsLimit ?? 0;
        return Math.Max(0, limit - CurrentDailyUsage);
    }

    /// <summary>
    /// Calculates percentage of quota used.
    /// </summary>
    public decimal GetUsagePercentage()
    {
        var limit = DailyGenerationLimit ?? DailyCreditsLimit ?? 1;
        if (limit == 0) return 0;
        return ((decimal)CurrentDailyUsage / limit) * 100;
    }

    /// <summary>
    /// Checks if quota is exhausted.
    /// </summary>
    public bool IsQuotaExhausted()
    {
        if (DailyGenerationLimit.HasValue && CurrentDailyUsage >= DailyGenerationLimit.Value)
            return true;
        if (DailyCreditsLimit.HasValue && CurrentDailyUsage >= DailyCreditsLimit.Value)
            return true;
        if (DailyCostLimit.HasValue && CurrentDailyCost >= DailyCostLimit.Value)
            return true;
        return false;
    }
}
