namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de resposta com resultado do envio de campanha de outreach.
/// </summary>
public class OutreachSendResultResponse
{
    public Guid? CampaignId { get; set; }
    public int QueuedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> SkippedReasons { get; set; } = [];
}
