using Diax.Application.GoogleAnalytics.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.GoogleAnalytics;

public interface IGoogleAnalyticsService
{
    Ga4StatusResponse GetStatus();
    Task<Result<Ga4ReportResponse>> GetReportAsync(int days, CancellationToken ct = default);
}
