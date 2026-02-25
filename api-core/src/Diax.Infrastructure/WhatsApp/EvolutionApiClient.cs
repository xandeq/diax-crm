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

            var friendlyError = ParseEvolutionApiError(responseBody, (int)response.StatusCode);
            return new WhatsAppSendResult(false, null, friendlyError);
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
    /// Extrai uma mensagem de erro amigável a partir da resposta da Evolution API.
    /// </summary>
    private static string ParseEvolutionApiError(string responseBody, int statusCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);

            // Formato: { "response": { "message": ["Error: Connection Closed"] } }
            if (doc.RootElement.TryGetProperty("response", out var responseElement) &&
                responseElement.TryGetProperty("message", out var messageElement))
            {
                string? msg = null;

                if (messageElement.ValueKind == JsonValueKind.Array && messageElement.GetArrayLength() > 0)
                    msg = messageElement[0].GetString();
                else if (messageElement.ValueKind == JsonValueKind.String)
                    msg = messageElement.GetString();

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    // Mensagens amigáveis para erros conhecidos
                    if (msg.Contains("Connection Closed", StringComparison.OrdinalIgnoreCase))
                        return "Conexão WhatsApp fechada. Reconecte a instância no painel da Evolution API escaneando o QR Code novamente.";

                    if (msg.Contains("not ready", StringComparison.OrdinalIgnoreCase))
                        return "Instância WhatsApp não está pronta. Verifique a conexão no painel da Evolution API.";

                    if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return "Instância WhatsApp não encontrada. Verifique o nome da instância nas configurações.";

                    return msg;
                }
            }

            // Formato: { "error": "Bad Request" }
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var error = errorElement.GetString();
                if (!string.IsNullOrWhiteSpace(error))
                    return $"Erro da Evolution API: {error}";
            }
        }
        catch
        {
            // Se não conseguir parsear, retorna mensagem genérica
        }

        return $"Erro ao enviar WhatsApp (HTTP {statusCode}). Verifique a conexão da instância.";
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
