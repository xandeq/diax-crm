using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class MailjetEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly MailjetSettings _settings;
    private readonly ILogger<MailjetEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MailjetEmailSender(
        HttpClient httpClient,
        IOptions<MailjetSettings> settings,
        ILogger<MailjetEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            return EmailSendResult.Fail("Mailjet API key/secret não configurados. Verifique a seção Mailjet no appsettings.");
        }

        try
        {
            var payload = new MailjetSendRequest
            {
                Messages =
                [
                    new MailjetMessage
                    {
                        From = new MailjetContact
                        {
                            Email = _settings.FromEmail,
                            Name = _settings.FromName
                        },
                        To =
                        [
                            new MailjetContact
                            {
                                Email = message.RecipientEmail,
                                Name = message.RecipientName
                            }
                        ],
                        Subject = message.Subject,
                        HtmlPart = message.HtmlBody
                    }
                ]
            };

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiKey}:{_settings.SecretKey}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailjet.com/v3.1/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MailjetSendResponse>(responseBody, JsonOptions);
                var messageId = result?.Messages?.FirstOrDefault()?.To?.FirstOrDefault()?.MessageId.ToString();

                _logger.LogInformation(
                    "Email enviado via Mailjet para {Recipient}. MessageId: {MessageId}",
                    message.RecipientEmail,
                    messageId);

                return EmailSendResult.Ok(messageId);
            }

            _logger.LogWarning(
                "Mailjet retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"Mailjet API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via Mailjet para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== Mailjet API DTOs =====

    private sealed class MailjetSendRequest
    {
        public List<MailjetMessage> Messages { get; set; } = [];
    }

    private sealed class MailjetMessage
    {
        public MailjetContact From { get; set; } = null!;
        public List<MailjetContact> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("HTMLPart")]
        public string HtmlPart { get; set; } = string.Empty;
    }

    private sealed class MailjetContact
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class MailjetSendResponse
    {
        public List<MailjetMessageResult>? Messages { get; set; }
    }

    private sealed class MailjetMessageResult
    {
        public List<MailjetRecipientResult>? To { get; set; }
    }

    private sealed class MailjetRecipientResult
    {
        public long MessageId { get; set; }
    }
}
