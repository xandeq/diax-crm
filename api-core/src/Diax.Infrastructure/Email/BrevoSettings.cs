namespace Diax.Infrastructure.Email;

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Alexandre Queiroz Marketing Digital";
    public string? ReplyTo { get; set; }
    public string? WebhookSecret { get; set; }
}
