using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<BrevoSettings> settings,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return EmailSendResult.Fail("Brevo API key não configurada. Verifique a seção Brevo no appsettings.");
        }

        try
        {
            var payload = new BrevoSendRequest
            {
                Sender = new BrevoEmailAddress
                {
                    Email = _settings.FromEmail,
                    Name = _settings.FromName
                },
                To = [new BrevoEmailAddress
                {
                    Email = message.RecipientEmail,
                    Name = message.RecipientName
                }],
                Subject = message.Subject,
                HtmlContent = message.HtmlBody
            };

            if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
            {
                payload.ReplyTo = new BrevoEmailAddress
                {
                    Email = _settings.ReplyTo,
                    Name = _settings.FromName
                };
            }

            if (message.Attachments.Count > 0)
            {
                payload.Attachment = message.Attachments.Select(a => new BrevoAttachment
                {
                    Name = a.FileName,
                    Content = a.Base64Content
                }).ToList();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _settings.ApiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<BrevoSendResponse>(responseBody, JsonOptions);
                _logger.LogInformation(
                    "Email enviado via Brevo para {Recipient}. MessageId: {MessageId}",
                    message.RecipientEmail,
                    result?.MessageId);

                return EmailSendResult.Ok(result?.MessageId);
            }

            _logger.LogWarning(
                "Brevo retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"Brevo API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via Brevo para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== Brevo API DTOs =====

    private sealed class BrevoSendRequest
    {
        public BrevoEmailAddress Sender { get; set; } = null!;
        public List<BrevoEmailAddress> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public BrevoEmailAddress? ReplyTo { get; set; }
        public List<BrevoAttachment>? Attachment { get; set; }
    }

    private sealed class BrevoEmailAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class BrevoAttachment
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class BrevoSendResponse
    {
        public string? MessageId { get; set; }
    }
}
