namespace Diax.Application.EmailMarketing.Dtos;

public class EmailAnalyticsSummaryResponse
{
    public OverallStatsDto OverallStats { get; set; } = new();
    public List<CampaignStatsDto> RecentCampaigns { get; set; } = [];
    public EngagementTrendDto EngagementTrend { get; set; } = new();
}

public class OverallStatsDto
{
    public int TotalCampaigns { get; set; }
    public int TotalEmailsSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicks { get; set; }
    public int TotalBounces { get; set; }
    public int TotalUnsubscribes { get; set; }

    public double OpenRate => TotalDelivered > 0 ? (double)TotalOpened / TotalDelivered * 100 : 0;
    public double ClickRate => TotalDelivered > 0 ? (double)TotalClicks / TotalDelivered * 100 : 0;
    public double BounceRate => TotalEmailsSent > 0 ? (double)TotalBounces / TotalEmailsSent * 100 : 0;
}

public class CampaignStatsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public int BounceCount { get; set; }
    public int UnsubscribeCount { get; set; }
    public int FailedCount { get; set; }

    public double OpenRate => DeliveredCount > 0 ? (double)OpenCount / DeliveredCount * 100 : 0;
    public double ClickRate => DeliveredCount > 0 ? (double)ClickCount / DeliveredCount * 100 : 0;
    public double ClickToOpenRate => OpenCount > 0 ? (double)ClickCount / OpenCount * 100 : 0;
}

public class EngagementTrendDto
{
    public List<DailyEngagementDto> DailyData { get; set; } = [];
}

public class DailyEngagementDto
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
}
