namespace Diax.Infrastructure.Email;

public class MailerSendSettings
{
    public string ApiToken { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Alexandre Queiroz Marketing Digital";
    public string? ReplyTo { get; set; }
}
