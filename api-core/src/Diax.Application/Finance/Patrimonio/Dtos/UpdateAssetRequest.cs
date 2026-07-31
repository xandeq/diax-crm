using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record UpdateAssetRequest(
    string Name,
    AssetClass Class,
    AssetOwnership Ownership,
    AssetLiquidity Liquidity,
    decimal CurrentValue,
    AssetValuationSource ValuationSource,
    string? Currency = null,
    decimal? CostBasis = null,
    DateTime? AcquiredAt = null,
    string? Notes = null
);
