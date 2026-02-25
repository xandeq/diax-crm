using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Application.WhatsApp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.WhatsApp;

/// <summary>
/// Cliente para a Evolution API v2.3.7 (WhatsApp via Baileys).
/// </summary>
public class EvolutionApiClient : IWhatsAppSender
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiSettings _settings;
    private readonly ILogger<EvolutionApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public EvolutionApiClient(
        HttpClient httpClient,
        IOptions<EvolutionApiSettings> settings,
        ILogger<EvolutionApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure base URL and default headers
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
    }

    /// <summary>
    /// Envia uma mensagem de texto via WhatsApp usando a Evolution API.
    /// </summary>
    public async Task<WhatsAppSendResult> SendTextAsync(
        string whatsappNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var formattedNumber = FormatPhoneNumber(whatsappNumber);
            if (string.IsNullOrWhiteSpace(formattedNumber))
            {
                _logger.LogWarning("Número de WhatsApp inválido: {Number}", whatsappNumber);
                return new WhatsAppSendResult(false, null, "Número de WhatsApp inválido.");
            }

            _logger.LogInformation("Enviando WhatsApp para {Number} via Evolution API", formattedNumber);

            var encodedInstance = Uri.EscapeDataString(_settings.InstanceName);
            var payload = new
            {
                number = formattedNumber,
                text = message
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"message/sendText/{encodedInstance}",
                payload,
                JsonOptions,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Evolution API retorna o messageId na resposta
                string? messageId = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("key", out var keyElement) &&
                        keyElement.TryGetProperty("id", out var idElement))
                    {
                        messageId = idElement.GetString();
                    }
                }
                catch { /* ignore parse errors */ }

                _logger.LogInformation("WhatsApp enviado com sucesso para {Number}. MessageId: {MessageId}",
                    formattedNumber, messageId ?? "N/A");

                return new WhatsAppSendResult(true, messageId, null);
            }

            _logger.LogError("Falha ao enviar WhatsApp para {Number}. Status: {Status}. Response: {Response}",
                formattedNumber, response.StatusCode, responseBody);

            return new WhatsAppSendResult(false, null, $"HTTP {(int)response.StatusCode}: {responseBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar WhatsApp para {Number}", whatsappNumber);
            return new WhatsAppSendResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Verifica o status de conexão da instância WhatsApp.
    /// </summary>
    public async Task<WhatsAppConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedInstance = Uri.EscapeDataString(_settings.InstanceName);
            var response = await _httpClient.GetAsync(
                $"instance/connectionState/{encodedInstance}",
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var state = "unknown";

                if (doc.RootElement.TryGetProperty("instance", out var instanceElement))
                {
                    if (instanceElement.TryGetProperty("state", out var stateElement))
                        state = stateElement.GetString() ?? "unknown";
                }
                else if (doc.RootElement.TryGetProperty("state", out var stateElement))
                {
                    state = stateElement.GetString() ?? "unknown";
                }

                var isConnected = state.Equals("open", StringComparison.OrdinalIgnoreCase);

                return new WhatsAppConnectionStatus(isConnected, state, _settings.InstanceName);
            }

            _logger.LogWarning("Falha ao verificar conexão WhatsApp. Status: {Status}", response.StatusCode);
            return new WhatsAppConnectionStatus(false, "error", _settings.InstanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar conexão WhatsApp");
            return new WhatsAppConnectionStatus(false, "error", _settings.InstanceName);
        }
    }

    /// <summary>
    /// Formata o número de telefone para o padrão da Evolution API.
    /// Remove "+", espaços, traços. Garante formato: 5527920010738
    /// </summary>
    private static string? FormatPhoneNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return null;

        // Remove tudo que não é dígito
        var cleaned = new string(number.Where(char.IsDigit).ToArray());

        if (cleaned.Length < 10)
            return null;

        // Se não começa com 55 (Brasil), adiciona
        if (!cleaned.StartsWith("55"))
            cleaned = "55" + cleaned;

        return cleaned;
    }
}
