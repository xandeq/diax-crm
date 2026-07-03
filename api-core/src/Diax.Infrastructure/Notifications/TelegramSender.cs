using System.Net.Http.Json;
using Diax.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Notifications;

public class TelegramSender : ITelegramSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramSender> _logger;

    public TelegramSender(HttpClient httpClient, IConfiguration configuration, ILogger<TelegramSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string? BotToken => _configuration["Telegram:BotToken"];
    private string? ChatId => _configuration["Telegram:ChatId"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);

    public async Task<bool> SendAsync(string htmlText, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Telegram não configurado (Telegram:BotToken/ChatId) — mensagem descartada");
            return false;
        }

        try
        {
            // Telegram limita 4096 chars por mensagem
            if (htmlText.Length > 4000)
                htmlText = htmlText[..4000] + "\n…";

            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{BotToken}/sendMessage",
                new
                {
                    chat_id = ChatId,
                    text = htmlText,
                    parse_mode = "HTML",
                    disable_web_page_preview = true,
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Telegram sendMessage falhou: {Status} {Body}", (int)response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem ao Telegram");
            return false;
        }
    }
}
