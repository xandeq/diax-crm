namespace Diax.Application.GoogleAnalytics.Dtos;

public record Ga4StatusResponse(bool IsConfigured, string? PropertyId);

public record Ga4OverviewMetrics(
    long Sessions,
    long Users,
    long NewUsers,
    long PageViews,
    double BounceRate,
    double AvgSessionDuration);

public record Ga4TrafficSourceItem(string Channel, long Sessions, double Percentage);

public record Ga4TopPageItem(string Page, long PageViews, long Users);

public record Ga4DeviceItem(string Device, long Sessions, double Percentage);

public record Ga4TimeSeriesPoint(string Date, long Sessions, long Users);

public record Ga4ReportResponse(
    Ga4OverviewMetrics Overview,
    List<Ga4TrafficSourceItem> TrafficSources,
    List<Ga4TopPageItem> TopPages,
    List<Ga4DeviceItem> Devices,
    List<Ga4TimeSeriesPoint> TimeSeries,
    string StartDate,
    string EndDate);
