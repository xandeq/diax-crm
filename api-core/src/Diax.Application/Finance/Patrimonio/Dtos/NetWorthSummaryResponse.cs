using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

/// <summary>
/// Resumo consolidado do patrimônio.
/// F1: CurrentValue é tratado como valor equivalente em BRL informado pelo usuário.
/// </summary>
public record NetWorthSummaryResponse(
    decimal TotalRealizableBRL,
    decimal TotalContingentBRL,
    decimal TotalProjectedBRL,
    IReadOnlyList<NetWorthByClassRow> ByClass,
    IReadOnlyList<NetWorthByLiquidityRow> ByLiquidity,
    IReadOnlyList<NetWorthByOwnershipRow> ByOwnership,
    IReadOnlyList<NetWorthCurrencyRow> Currencies
);

public record NetWorthByClassRow(
    AssetClass Class,
    decimal TotalBRL,
    decimal Pct
);

public record NetWorthByLiquidityRow(
    AssetLiquidity Liquidity,
    decimal TotalBRL
);

public record NetWorthByOwnershipRow(
    AssetOwnership Ownership,
    decimal TotalBRL
);

public record NetWorthCurrencyRow(
    string Currency,
    decimal Total
);
