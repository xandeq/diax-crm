using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

/// <summary>Item externo do radar diário (achado de bem palpável/imóvel/veículo etc).</summary>
public record IngestOpportunityItem(
    AssetClass Class,
    string Title,
    string Thesis,
    string? Risk = null,
    decimal? Score = null,
    int? EaseRank = null,
    string? Ticker = null
);

/// <summary>Lote de oportunidades externas para injetar no dia corrente.</summary>
public record IngestOpportunitiesRequest(
    List<IngestOpportunityItem> Items
);
