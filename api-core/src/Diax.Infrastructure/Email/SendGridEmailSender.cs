using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SendGridEmailSender(
        HttpClient httpClient,
        IOptions<SendGridSettings> settings,
        ILogger<SendGridEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return EmailSendResult.Fail("SendGrid API key não configurada. Verifique a seção SendGrid no appsettings.");

        try
        {
            // Pass campaign ID as custom_arg so webhook events can look up the campaign
            var customArgs = message.Tags?.Count > 0
                ? new Dictionary<string, string> { ["campaign_id"] = message.Tags[0] }
                : null;

            var payload = new SendGridRequest
            {
                Personalizations = [new SendGridPersonalization
                {
                    To = [new SendGridAddress { Email = message.RecipientEmail, Name = message.RecipientName }],
                    CustomArgs = customArgs
                }],
                From = new SendGridAddress
                {
                    Email = _settings.FromEmail,
                    Name = _settings.FromName
                },
                Subject = message.Subject,
                Content = [new SendGridContent { Type = "text/html", Value = message.HtmlBody }]
            };

            if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
            {
                payload.ReplyTo = new SendGridAddress { Email = _settings.ReplyTo };
            }

            if (message.Attachments.Count > 0)
            {
                payload.Attachments = message.Attachments.Select(a => new SendGridAttachment
                {
                    Content = a.Base64Content,
                    Filename = a.FileName,
                    Type = "application/octet-stream",
                    Disposition = "attachment"
                }).ToList();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                    ? values.FirstOrDefault()
                    : null;

                _logger.LogInformation(
                    "Email enviado via SendGrid para {Recipient}. MessageId: {MessageId}",
                    message.RecipientEmail,
                    messageId);

                return EmailSendResult.Ok(messageId);
            }

            _logger.LogWarning(
                "SendGrid retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"SendGrid API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via SendGrid para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== SendGrid API DTOs =====

    private sealed class SendGridRequest
    {
        public List<SendGridPersonalization> Personalizations { get; set; } = [];
        public SendGridAddress From { get; set; } = null!;
        public string Subject { get; set; } = string.Empty;
        public List<SendGridContent> Content { get; set; } = [];
        public SendGridAddress? ReplyTo { get; set; }
        public List<SendGridAttachment>? Attachments { get; set; }
    }

    private sealed class SendGridPersonalization
    {
        public List<SendGridAddress> To { get; set; } = [];

        [JsonPropertyName("custom_args")]
        public Dictionary<string, string>? CustomArgs { get; set; }
    }

    private sealed class SendGridAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class SendGridContent
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private sealed class SendGridAttachment
    {
        public string Content { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Disposition { get; set; } = string.Empty;
    }
}
