namespace Diax.Application.Customers.Dtos;

/// <summary>
/// Estatísticas agregadas de email para um contato específico (via API Brevo).
/// </summary>
public class ContactEmailStatsResponse
{
    public string Email { get; set; } = string.Empty;
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalBounced { get; set; }
    public int TotalUnsubscribed { get; set; }

    /// <summary>
    /// Taxa de abertura (opens / delivered * 100)
    /// </summary>
    public double OpenRate => TotalDelivered > 0 ? (double)TotalOpened / TotalDelivered * 100 : 0;

    /// <summary>
    /// Taxa de cliques (clicks / delivered * 100)
    /// </summary>
    public double ClickRate => TotalDelivered > 0 ? (double)TotalClicked / TotalDelivered * 100 : 0;

    /// <summary>
    /// Nível de engajamento baseado na open rate
    /// </summary>
    public EngagementLevel EngagementLevel => OpenRate switch
    {
        > 50 => EngagementLevel.High,
        > 20 => EngagementLevel.Medium,
        _ => EngagementLevel.Low
    };

    /// <summary>
    /// Última vez que foi calculado (para cache)
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Nível de engajamento do contato
/// </summary>
public enum EngagementLevel
{
    Low = 0,     // 0-20% open rate - 🔴
    Medium = 1,  // 20-50% open rate - 🟡
    High = 2     // >50% open rate - 🟢
}

/// <summary>
/// Timeline de eventos de email para um contato
/// </summary>
public class EmailTimelineResponse
{
    public string Email { get; set; } = string.Empty;
    public List<EmailEventDto> Events { get; set; } = [];
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Evento individual de email
/// </summary>
public class EmailEventDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailEventType Event { get; set; }
    public DateTime EventAt { get; set; }
    public string? CampaignName { get; set; }
    public Guid? CampaignId { get; set; }
    public string? Link { get; set; } // Para eventos de click
    public string? Reason { get; set; } // Para bounces
}

/// <summary>
/// Tipo de evento de email
/// </summary>
public enum EmailEventType
{
    Sent = 0,
    Delivered = 1,
    Opened = 2,
    Clicked = 3,
    Bounced = 4,
    Spam = 5,
    Unsubscribed = 6
}

/// <summary>
/// Resposta da API Brevo para estatísticas de contato
/// Endpoint: GET /contacts/{email}/campaignStats
/// </summary>
public class BrevoContactStatsApiResponse
{
    public List<CampaignStats> CampaignStats { get; set; } = [];
}

public class CampaignStats
{
    public int Sent { get; set; }
    public int HardBounces { get; set; }
    public int SoftBounces { get; set; }
    public int Complaints { get; set; }
    public int Unsubscriptions { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
    public int Delivered { get; set; }
}

/// <summary>
/// Resposta da API Brevo para eventos de email
/// Endpoint: GET /smtp/statistics/events
/// </summary>
public class BrevoEmailEventsApiResponse
{
    public List<BrevoEmailEvent> Events { get; set; } = [];
}

public class BrevoEmailEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public long Date { get; set; } // Unix timestamp
    public string? Tag { get; set; }
    public string? Link { get; set; }
    public string? Reason { get; set; }
}
