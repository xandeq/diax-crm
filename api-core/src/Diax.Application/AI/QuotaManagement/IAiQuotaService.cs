using Diax.Shared.Results;

namespace Diax.Application.AI.QuotaManagement;

public interface IAiQuotaService
{
    Task<Result<bool>> CanUserGenerateAsync(Guid providerId, int creditsOrGenerations = 1, CancellationToken ct = default);
    Task<Result> RecordGenerationAsync(Guid providerId, int creditsOrGenerations = 1, CancellationToken ct = default);
    Task<QuotaStatusDto?> GetQuotaStatusAsync(Guid providerId, CancellationToken ct = default);
    Task ResetDailyQuotasAsync(CancellationToken ct = default);
}

public class QuotaStatusDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; }
    public string QuotaType { get; set; } // "Generations", "Credits", "Minutes", "Cost"
    public int? DailyLimit { get; set; }
    public int CurrentUsage { get; set; }
    public int Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public DateTime ResetAt { get; set; }
    public TimeSpan TimeUntilReset { get; set; }
    public bool IsExhausted { get; set; }
}
