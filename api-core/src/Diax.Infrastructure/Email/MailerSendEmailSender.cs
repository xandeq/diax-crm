using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class MailerSendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly MailerSendSettings _settings;
    private readonly ILogger<MailerSendEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MailerSendEmailSender(
        HttpClient httpClient,
        IOptions<MailerSendSettings> settings,
        ILogger<MailerSendEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            return EmailSendResult.Fail("MailerSend API token não configurado. Verifique a seção MailerSend no appsettings.");

        try
        {
            var payload = new MailerSendRequest
            {
                From = new MailerSendAddress
                {
                    Email = _settings.FromEmail,
                    Name = _settings.FromName
                },
                To = [new MailerSendAddress
                {
                    Email = message.RecipientEmail,
                    Name = message.RecipientName
                }],
                Subject = message.Subject,
                Html = message.HtmlBody
            };

            if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
            {
                payload.ReplyTo = new MailerSendAddress { Email = _settings.ReplyTo };
            }

            if (message.Attachments.Count > 0)
            {
                payload.Attachments = message.Attachments.Select(a => new MailerSendAttachment
                {
                    Filename = a.FileName,
                    Content = a.Base64Content,
                    Disposition = "attachment"
                }).ToList();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailersend.com/v1/email");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                    ? values.FirstOrDefault()
                    : null;

                _logger.LogInformation(
                    "Email enviado via MailerSend para {Recipient}. MessageId: {MessageId}",
                    message.RecipientEmail,
                    messageId);

                return EmailSendResult.Ok(messageId);
            }

            _logger.LogWarning(
                "MailerSend retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"MailerSend API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via MailerSend para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== MailerSend API DTOs =====

    private sealed class MailerSendRequest
    {
        public MailerSendAddress From { get; set; } = null!;
        public List<MailerSendAddress> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public MailerSendAddress? ReplyTo { get; set; }
        public List<MailerSendAttachment>? Attachments { get; set; }
    }

    private sealed class MailerSendAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class MailerSendAttachment
    {
        public string Filename { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Disposition { get; set; } = "attachment";
    }
}
