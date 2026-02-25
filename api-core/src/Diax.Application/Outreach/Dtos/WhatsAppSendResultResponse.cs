namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// Resultado do envio em massa de WhatsApp.
/// </summary>
public class WhatsAppSendResultResponse
{
    public int SentCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> SkippedReasons { get; set; } = new();
    public List<string> FailedReasons { get; set; } = new();
}
