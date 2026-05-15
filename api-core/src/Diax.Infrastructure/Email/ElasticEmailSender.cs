using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class ElasticEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ElasticEmailSettings _settings;
    private readonly ILogger<ElasticEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ElasticEmailSender(
        HttpClient httpClient,
        IOptions<ElasticEmailSettings> settings,
        ILogger<ElasticEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return EmailSendResult.Fail("ElasticEmail API key não configurada. Verifique a seção ElasticEmail no appsettings.");

        try
        {
            var payload = new ElasticEmailRequest
            {
                Recipients = new ElasticEmailRecipients
                {
                    To = [new ElasticEmailContact
                    {
                        Email = message.RecipientEmail,
                        Fields = new Dictionary<string, string>
                        {
                            ["name"] = message.RecipientName ?? string.Empty
                        }
                    }]
                },
                Content = new ElasticEmailContent
                {
                    Body = [new ElasticEmailBody { ContentType = "HTML", Content = message.HtmlBody }],
                    From = string.IsNullOrWhiteSpace(_settings.FromName)
                        ? _settings.FromEmail
                        : $"{_settings.FromName} <{_settings.FromEmail}>",
                    ReplyTo = _settings.ReplyTo,
                    Subject = message.Subject
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.elasticemail.com/v4/emails/transactional");
            request.Headers.Add("X-ElasticEmail-ApiKey", _settings.ApiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ElasticEmailResponse>(responseBody, JsonOptions);
                _logger.LogInformation(
                    "Email enviado via ElasticEmail para {Recipient}. TransactionId: {TransactionId}",
                    message.RecipientEmail,
                    result?.TransactionId);

                return EmailSendResult.Ok(result?.TransactionId);
            }

            _logger.LogWarning(
                "ElasticEmail retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"ElasticEmail API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via ElasticEmail para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== ElasticEmail API DTOs =====

    private sealed class ElasticEmailRequest
    {
        public ElasticEmailRecipients Recipients { get; set; } = null!;
        public ElasticEmailContent Content { get; set; } = null!;
    }

    private sealed class ElasticEmailRecipients
    {
        public List<ElasticEmailContact> To { get; set; } = [];
    }

    private sealed class ElasticEmailContact
    {
        public string Email { get; set; } = string.Empty;
        public Dictionary<string, string>? Fields { get; set; }
    }

    private sealed class ElasticEmailContent
    {
        public List<ElasticEmailBody> Body { get; set; } = [];
        public string From { get; set; } = string.Empty;
        public string? ReplyTo { get; set; }
        public string Subject { get; set; } = string.Empty;
    }

    private sealed class ElasticEmailBody
    {
        public string ContentType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ElasticEmailResponse
    {
        public string? TransactionId { get; set; }
    }
}
