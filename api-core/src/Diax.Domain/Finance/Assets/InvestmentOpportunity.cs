using Diax.Domain.Common;

namespace Diax.Domain.Finance.Assets;

/// <summary>
/// Oportunidade diária de investimento do módulo Patrimônio (F2 — camada de inteligência).
/// Gerada uma vez por dia por usuário, a partir do InvestIQ (Source="investiq")
/// e de uma lista curada de ideias (Source="idea").
/// </summary>
public class InvestmentOpportunity : AuditableEntity, IUserOwnedEntity
{
    public const string SourceInvestIQ = "investiq";
    public const string SourceIdea = "idea";

    public Guid UserId { get; private set; }
    public AssetClass Class { get; private set; }
    public string? Ticker { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Thesis { get; private set; } = string.Empty;

    /// <summary>
    /// Ranking de facilidade de execução (1 = mais fácil). Nulo para oportunidades do InvestIQ.
    /// </summary>
    public int? EaseRank { get; private set; }

    public decimal Score { get; private set; }
    public decimal? SuggestedAllocationPct { get; private set; }

    /// <summary>
    /// Origem da oportunidade: "investiq" | "idea".
    /// </summary>
    public string Source { get; private set; } = SourceIdea;

    /// <summary>
    /// Nível de risco: "baixo" | "medio" | "alto".
    /// </summary>
    public string Risk { get; private set; } = string.Empty;

    public DateTime GeneratedAt { get; private set; }

    private InvestmentOpportunity() { }

    public static InvestmentOpportunity Create(
        Guid userId,
        AssetClass assetClass,
        string title,
        string thesis,
        string source,
        string risk,
        DateTime generatedAt,
        decimal score = 0m,
        string? ticker = null,
        int? easeRank = null,
        decimal? suggestedAllocationPct = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Opportunity title cannot be empty", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("Opportunity title cannot exceed 200 characters", nameof(title));

        if (source != SourceInvestIQ && source != SourceIdea)
            throw new ArgumentException($"Source must be '{SourceInvestIQ}' or '{SourceIdea}'", nameof(source));

        return new InvestmentOpportunity
        {
            UserId = userId,
            Class = assetClass,
            Ticker = ticker,
            Title = title,
            Thesis = thesis ?? string.Empty,
            EaseRank = easeRank,
            Score = score,
            SuggestedAllocationPct = suggestedAllocationPct,
            Source = source,
            Risk = risk ?? string.Empty,
            GeneratedAt = generatedAt
        };
    }
}
