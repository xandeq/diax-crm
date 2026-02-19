using System.Net;
using System.Net.Mail;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost)
            || string.IsNullOrWhiteSpace(_settings.SmtpUsername)
            || string.IsNullOrWhiteSpace(_settings.SmtpPassword)
            || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            return EmailSendResult.Fail("Configuração SMTP incompleta. Verifique a seção Email no appsettings.");
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
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

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
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
