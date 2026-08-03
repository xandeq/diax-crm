using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record AssetResponse(
    Guid Id,
    string Name,
    AssetClass Class,
    AssetOwnership Ownership,
    AssetLiquidity Liquidity,
    string Currency,
    decimal CurrentValue,
    decimal? CostBasis,
    DateTime? AcquiredAt,
    AssetValuationSource ValuationSource,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<AssetValuationResponse>? Valuations = null,
    FipeLinkResponse? Fipe = null
);
