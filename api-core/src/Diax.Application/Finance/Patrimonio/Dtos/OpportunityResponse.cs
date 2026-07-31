using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record OpportunityResponse(
    Guid Id,
    AssetClass Class,
    string? Ticker,
    string Title,
    string Thesis,
    int? EaseRank,
    decimal Score,
    decimal? SuggestedAllocationPct,
    string Source,
    string Risk,
    DateTime GeneratedAt
);
