namespace Diax.Infrastructure.Email;

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Alexandre Queiroz Marketing Digital";
    public string? ReplyTo { get; set; }
}
