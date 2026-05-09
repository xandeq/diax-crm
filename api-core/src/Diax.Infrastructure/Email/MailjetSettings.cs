namespace Diax.Infrastructure.Email;

public class MailjetSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "DIAX CRM";
    public string? WebhookSecret { get; set; }
}
