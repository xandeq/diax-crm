using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class MailtrapEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly MailtrapSettings _settings;
    private readonly ILogger<MailtrapEmailSender> _logger;

    private const string SendEndpoint = "https://send.api.mailtrap.io/api/send";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MailtrapEmailSender(
        HttpClient httpClient,
        IOptions<MailtrapSettings> settings,
        ILogger<MailtrapEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            return EmailSendResult.Fail("Mailtrap API token não configurado. Verifique a seção Mailtrap no appsettings.");

        try
        {
            var payload = new MailtrapSendRequest
            {
                From = new MailtrapAddress
                {
                    Email = _settings.FromEmail,
                    Name = _settings.FromName
                },
                To = [new MailtrapAddress
                {
                    Email = message.RecipientEmail,
                    Name = message.RecipientName
                }],
                Subject = message.Subject,
                Html = message.HtmlBody,
                // Mailtrap aceita apenas uma categoria por envio
                Category = message.Tags?.FirstOrDefault()
            };

            if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
                payload.ReplyTo = new MailtrapAddress { Email = _settings.ReplyTo };

            if (message.Attachments.Count > 0)
            {
                payload.Attachments = message.Attachments.Select(a => new MailtrapAttachment
                {
                    Filename = a.FileName,
                    Content = a.Base64Content,
                    Disposition = "attachment"
                }).ToList();
            }

            return await PostAsync(payload, message.RecipientEmail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via Mailtrap para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            return EmailSendResult.Fail("Mailtrap API token não configurado.");

        try
        {
            var payload = new MailtrapSendRequest
            {
                From = new MailtrapAddress { Email = message.From.Address, Name = message.From.Display },
                To = message.To.Select(a => new MailtrapAddress { Email = a.Address, Name = a.Display }).ToList(),
                Subject = message.Subject,
                Html = message.Html,
                Text = message.Text,
                Category = message.Tags?.FirstOrDefault()
            };

            if (message.ReplyTo.HasValue)
                payload.ReplyTo = new MailtrapAddress { Email = message.ReplyTo.Value.Address };

            if (message.Attachments?.Count > 0)
                payload.Attachments = message.Attachments.Select(a => new MailtrapAttachment
                {
                    Filename = a.FileName,
                    Content = a.Base64Content,
                    Disposition = "attachment"
                }).ToList();

            return await PostAsync(payload, message.To.Select(a => a.Address).FirstOrDefault(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via Mailtrap (EmailMessage) para {Recipient}", message.From.Address);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    private async Task<EmailSendResult> PostAsync(MailtrapSendRequest payload, string? recipient, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<MailtrapSendResponse>(responseBody, JsonOptions);
            var messageId = result?.MessageIds?.FirstOrDefault();

            _logger.LogInformation(
                "Email enviado via Mailtrap para {Recipient}. MessageId: {MessageId}",
                recipient,
                messageId);

            return EmailSendResult.Ok(messageId);
        }

        _logger.LogWarning(
            "Mailtrap retornou {StatusCode} para {Recipient}: {Body}",
            (int)response.StatusCode,
            recipient,
            responseBody);

        return EmailSendResult.Fail($"Mailtrap API error {(int)response.StatusCode}: {responseBody}");
    }

    // ===== Mailtrap API DTOs (snake_case) =====

    private sealed class MailtrapSendRequest
    {
        public MailtrapAddress From { get; set; } = null!;
        public List<MailtrapAddress> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? Text { get; set; }
        public MailtrapAddress? ReplyTo { get; set; }
        public List<MailtrapAttachment>? Attachments { get; set; }
        public string? Category { get; set; }
    }

    private sealed class MailtrapAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class MailtrapAttachment
    {
        public string Filename { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Disposition { get; set; } = "attachment";
    }

    private sealed class MailtrapSendResponse
    {
        public bool Success { get; set; }
        public List<string>? MessageIds { get; set; }
    }
}
