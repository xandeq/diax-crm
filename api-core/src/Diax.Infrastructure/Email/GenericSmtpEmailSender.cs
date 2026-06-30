using System.Net;
using System.Net.Mail;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Email;

public class GenericSmtpEmailSender : IEmailSender
{
    private readonly SmtpProviderSettings _settings;
    private readonly ILogger<GenericSmtpEmailSender> _logger;

    public GenericSmtpEmailSender(SmtpProviderSettings settings, ILogger<GenericSmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.Password)
            || string.IsNullOrWhiteSpace(_settings.DefaultFromEmail))
        {
            return EmailSendResult.Fail("Configuração SMTP incompleta. Verifique Host, Username, Password e DefaultFromEmail.");
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.DefaultFromEmail, _settings.DefaultFromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(message.RecipientEmail, message.RecipientName));

            foreach (var attachment in message.Attachments)
            {
                var bytes = Convert.FromBase64String(attachment.Base64Content);
                var stream = new MemoryStream(bytes);
                var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                mailMessage.Attachments.Add(mailAttachment);
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mailMessage);

            return EmailSendResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }
}
