using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendSettings> settings,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return EmailSendResult.Fail("Resend API key não configurada. Verifique a seção Resend no appsettings.");
        }

        try
        {
            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromName)
                ? _settings.FromEmail
                : $"{_settings.FromName} <{_settings.FromEmail}>";

            var payload = new ResendSendRequest
            {
                From = fromAddress,
                To = [message.RecipientEmail],
                Subject = message.Subject,
                Html = message.HtmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ResendSendResponse>(responseBody, JsonOptions);

                _logger.LogInformation(
                    "Email enviado via Resend para {Recipient}. Id: {MessageId}",
                    message.RecipientEmail,
                    result?.Id);

                return EmailSendResult.Ok(result?.Id);
            }

            _logger.LogWarning(
                "Resend retornou {StatusCode} para {Recipient}: {Body}",
                (int)response.StatusCode,
                message.RecipientEmail,
                responseBody);

            return EmailSendResult.Fail($"Resend API error {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar email via Resend para {Recipient}", message.RecipientEmail);
            return EmailSendResult.Fail(ex.Message);
        }
    }

    // ===== Resend API DTOs =====

    private sealed class ResendSendRequest
    {
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
    }

    private sealed class ResendSendResponse
    {
        public string? Id { get; set; }
    }
}
