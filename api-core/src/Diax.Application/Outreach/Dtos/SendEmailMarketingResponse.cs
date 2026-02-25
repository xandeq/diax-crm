namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// Resposta do envio de email marketing em massa.
/// </summary>
public class SendEmailMarketingResponse
{
    public Guid CampaignId { get; set; }
    public int TotalValidContacts { get; set; }
    public int QueuedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> SkippedReasons { get; set; } = [];
}
