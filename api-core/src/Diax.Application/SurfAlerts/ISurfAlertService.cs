using Diax.Application.SurfAlerts.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.SurfAlerts;

public interface ISurfAlertService
{
    Task<Result<IngestSurfAlertResponse>> IngestAsync(
        string apiKeyPlain, string? idempotencyKey, IngestSurfAlertRequest request, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SurfAlertResponse>>> GetRecentAsync(int limit, CancellationToken ct = default);
}
