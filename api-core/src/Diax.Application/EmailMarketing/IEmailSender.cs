namespace Diax.Application.EmailMarketing;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default);
}
