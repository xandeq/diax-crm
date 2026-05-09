namespace Diax.Application.EmailMarketing.Pro.Dtos;

public record ProviderHealthDto
{
    public string Provider { get; init; } = string.Empty;
    public int SentToday { get; init; }
    public int DailyLimit { get; init; }
    public int DailyRemaining { get; init; }
    public double DailyUsagePercent { get; init; }
    public int SentThisHour { get; init; }
    public int HourlyLimit { get; init; }
    public int HourlyRemaining { get; init; }
    public string Health { get; init; } = "ok"; // ok | degraded | down
}
