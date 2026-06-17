using System;
using System.Collections.Generic;

namespace Diax.Application.EmailMarketing.Dtos;

public class PilotStatusResponse
{
    public bool IsCircuitBreakerOpen { get; set; }
    public string? CircuitBreakerReason { get; set; }
    public double CurrentErrorRate { get; set; }
    public int WebhookFailureCount { get; set; }
    public bool CampaignReadinessPassed { get; set; }
    public string? CampaignReadinessError { get; set; }
    public List<PilotEventDto> RecentEvents { get; set; } = new();
}

public class PilotEventDto
{
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? BlockingReasons { get; set; }
    public int LeadCount { get; set; }
    public bool DryRun { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string UserEmail { get; set; } = string.Empty;
}
