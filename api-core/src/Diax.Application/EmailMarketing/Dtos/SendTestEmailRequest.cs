namespace Diax.Application.EmailMarketing.Dtos;

public class SendTestEmailRequest
{
    public string? SubjectOverride { get; set; }
    public string? BodyHtmlOverride { get; set; }
}
