namespace Diax.Application.Finance.Patrimonio.Dtos;

/// <summary>
/// Cotações do dia para bens palpáveis e moeda forte (fonte: AwesomeAPI, best-effort).
/// Campos nulos quando a fonte não retornou aquele par.
/// </summary>
public record MarketSnapshotResponse(
    decimal? UsdBrl,
    decimal? EurBrl,
    decimal? GbpBrl,
    decimal? BtcBrl,
    decimal? GoldOunceBrl,
    decimal? GoldGramBrl,
    DateTime FetchedAt
);
