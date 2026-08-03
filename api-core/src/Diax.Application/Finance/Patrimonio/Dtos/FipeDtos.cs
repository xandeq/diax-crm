namespace Diax.Application.Finance.Patrimonio.Dtos;

/// <summary>Item genérico do catálogo FIPE (marca, modelo ou ano): código + nome.</summary>
public record FipeItemResponse(string Code, string Name);

/// <summary>Preço FIPE de um veículo específico (marca/modelo/ano).</summary>
public record FipePriceResponse(
    decimal Price,
    string Brand,
    string Model,
    int ModelYear,
    string? Fuel,
    string? CodeFipe,
    string? ReferenceMonth
);

/// <summary>Códigos FIPE vinculados a um ativo Veiculo (presente no AssetResponse).</summary>
public record FipeLinkResponse(
    string VehicleType,
    string BrandCode,
    string ModelCode,
    string YearCode
);

/// <summary>Vincula um ativo Veiculo aos códigos FIPE do drill-down.</summary>
public record LinkFipeRequest(
    string VehicleType,
    string BrandCode,
    string ModelCode,
    string YearCode
);

/// <summary>Resultado do vínculo: ativo já reavaliado com o preço FIPE atual.</summary>
public record LinkFipeResponse(
    Guid AssetId,
    decimal Value,
    string? ReferenceMonth,
    string Model
);

/// <summary>Resumo do refresh mensal lazy dos veículos vinculados.</summary>
public record FipeRefreshResponse(
    int Checked,
    int Updated
);
