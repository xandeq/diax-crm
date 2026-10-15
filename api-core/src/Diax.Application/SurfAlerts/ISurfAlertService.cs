using Diax.Application.SurfAlerts.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.SurfAlerts;

public interface ISurfAlertService
{
    Task<Result<IngestSurfAlertResponse>> IngestAsync(
        string apiKeyPlain, string? idempotencyKey, IngestSurfAlertRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lista os alertas mais recentes. <paramref name="apiKeyPlain"/> nulo pula a checagem de
    /// escopo (chamador já autenticado/autorizado por JWT no controller); não-nulo exige o
    /// escopo 'surf-alerts.view'.
    /// </summary>
    Task<Result<IReadOnlyList<SurfAlertResponse>>> GetRecentAsync(
        string? apiKeyPlain, int limit, CancellationToken ct = default);
}
