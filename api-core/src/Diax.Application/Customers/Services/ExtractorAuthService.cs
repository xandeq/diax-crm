using System.Text;
using System.Text.Json;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers.Services;

/// <summary>
/// Serviço para autenticar com Extrator e gerenciar token.
/// Handles login automático e refresh de token quando necessário.
/// </summary>
public interface IExtractorAuthService
{
    /// <summary>
    /// Obtém token válido — se expirado, faz login automático.
    /// </summary>
    Task<Result<string>> GetValidTokenAsync();

    /// <summary>
    /// Força refresh do token (logout + login).
    /// </summary>
    Task<Result<string>> RefreshTokenAsync();
}

public class ExtractorAuthService : IExtractorAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExtractorAuthService> _logger;

    // Credenciais do Extrator (vêm do appsettings ou AWS SM)
    private readonly string _extractorUrl;
    private readonly string _username;
    private readonly string _password;

    private const string TOKEN_CACHE_KEY = "extrator_auth_token";
    private const int TOKEN_CACHE_DURATION_MINUTES = 55; // Token expira em ~1h, refresh em 55min

    public ExtractorAuthService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ExtractorAuthService> logger,
        string extractorUrl,
        string username,
        string password)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _extractorUrl = extractorUrl?.TrimEnd('/') ?? "";
        _username = username ?? "";
        _password = password ?? "";
    }

    public async Task<Result<string>> GetValidTokenAsync()
    {
        // ✅ Check cache first
        if (_cache.TryGetValue(TOKEN_CACHE_KEY, out var cachedToken))
        {
            _logger.LogDebug("✓ Extrator token loaded from cache");
            return Result.Success<string>((string)cachedToken);
        }

        _logger.LogInformation("🔑 Token expirado ou não encontrado, fazendo login automático no Extrator...");

        // ✗ Token expirado, fazer login
        return await RefreshTokenAsync();
    }

    public async Task<Result<string>> RefreshTokenAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_extractorUrl) ||
                string.IsNullOrWhiteSpace(_username) ||
                string.IsNullOrWhiteSpace(_password))
            {
                _logger.LogError("❌ Credenciais do Extrator não configuradas (URL, username, password necessários)");
                return Result.Failure<string>(new Error(
                    "ExtractorCredentialsNotFound",
                    "Credenciais do Extrator não estão configuradas"));
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Payload do login
            var loginPayload = new
            {
                username = _username,
                password = _password
            };

            var jsonPayload = JsonSerializer.Serialize(loginPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var loginUrl = $"{_extractorUrl}/api/login";

            _logger.LogInformation("🔐 Autenticando com Extrator ({Url})...", loginUrl);

            var response = await client.PostAsync(loginUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Falha no login do Extrator: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                return Result.Failure<string>(new Error(
                    "ExtractorLoginFailed",
                    $"Falha ao autenticar com Extrator: {response.StatusCode}"));
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<ExtractorLoginResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (loginResponse?.Token == null)
            {
                _logger.LogError("❌ Login bem-sucedido mas token não retornado");
                return Result.Failure<string>(new Error(
                    "ExtractorTokenNotFound",
                    "Resposta de login não contém token"));
            }

            var token = loginResponse.Token;

            // ✅ Cache token por 55 minutos
            _cache.Set(TOKEN_CACHE_KEY, token, TimeSpan.FromMinutes(TOKEN_CACHE_DURATION_MINUTES));

            _logger.LogInformation("✅ Token Extrator renovado com sucesso. Válido por {Minutes} minutos",
                TOKEN_CACHE_DURATION_MINUTES);

            return Result.Success(token);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Erro de rede ao conectar com Extrator");
            return Result.Failure<string>(new Error(
                "ExtractorConnectionError",
                $"Erro ao conectar com Extrator: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro inesperado ao autenticar com Extrator");
            return Result.Failure<string>(new Error(
                "ExtractorAuthError",
                $"Erro ao autenticar: {ex.Message}"));
        }
    }
}

/// <summary>
/// Resposta do endpoint /api/login do Extrator
/// </summary>
public class ExtractorLoginResponse
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public int? Id { get; set; }
}
