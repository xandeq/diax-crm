using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Diax.Shared.Interfaces;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Infrastructure.ExternalServices;

public class ConfigurationProvider : Diax.Shared.Interfaces.IConfigurationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationProvider> _logger;
    private readonly IMemoryCache _cache;
    private string _lastSource = "";

    private const string CACHE_KEY = "extractor_config";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    public ConfigurationProvider(
        IConfiguration configuration,
        ILogger<ConfigurationProvider> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Carrega configuração do Extrator em cascata resiliente (3 camadas).
    ///
    /// Cascata (ordem de prioridade):
    /// 1. AWS Secrets Manager (tools/diax-extrator) — dinâmica, refresh a cada 10 min
    /// 2. IConfiguration (appsettings.Production.json baked via CI/CD) — fallback offline-safe
    /// 3. Environment Variables (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN)
    ///
    /// Notas:
    /// - Se AWS SM falhar (offline/timeout), continua para camada 2/3 automaticamente
    /// - Resultado é cacheado por 5 minutos para performance
    /// - Garante que a app funciona mesmo sem AWS SM disponível
    /// </summary>
    public async Task<Result<(string url, string token)>> GetExtractorConfigAsync()
    {
        // ✅ Check cache first (defensive: _cache may be null if DI not configured)
        if (_cache != null && _cache.TryGetValue(CACHE_KEY, out var cached))
        {
            var (cachedUrl, cachedToken, cachedSource) = ((string, string, string))cached;
            _lastSource = cachedSource;
            _logger?.LogDebug("✓ Extrator config loaded from CACHE (source: {Source})", _lastSource);
            return Result.Success<(string, string)>((cachedUrl, cachedToken));
        }

        // 1️⃣ AWS Secrets Manager (primária - dinâmica)
        var awsResult = await TryLoadFromAwsSecretsManagerAsync();
        if (awsResult != null && awsResult.IsSuccess)
        {
            var (url, token) = awsResult.Value;
            _lastSource = "AWS Secrets Manager (tools/diax-extrator)";
            if (_cache != null)
                _cache.Set(CACHE_KEY, (url, token, _lastSource), CACHE_DURATION);
            _logger?.LogInformation("✓ Extrator config loaded from {Source}", _lastSource);
            return awsResult;
        }

        // 2️⃣ IConfiguration (appsettings.Production.json baked no CI/CD - fallback offline-safe)
        var configResult = TryLoadFromConfiguration();
        if (configResult.IsSuccess)
        {
            var (url, token) = configResult.Value;
            _lastSource = DetermineConfigurationSource();
            if (_cache != null)
                _cache.Set(CACHE_KEY, (url, token, _lastSource), CACHE_DURATION);
            _logger?.LogInformation("✓ Extrator config loaded from {Source} (AWS SM unavailable, using fallback)", _lastSource);
            return configResult;
        }

        // ❌ Nenhuma fonte disponível
        _lastSource = "NOT_FOUND";
        var errorMessage = @"
❌ Extrator configuration not found.

Cascata testada (em ordem):
  1. AWS Secrets Manager (tools/diax-extrator) - OFFLINE/UNAVAILABLE
  2. appsettings.Production.json (Extrator:Url, Extrator:ApiToken) - NÃO CONFIGURADO
  3. Environment Variables (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN) - NÃO CONFIGURADO

Configure em pelo menos UMA dessas fontes:
  - AWS SM: aws secretsmanager create-secret --name tools/diax-extrator
  - Env var: export DIAX_EXTRATOR_URL=... DIAX_EXTRATOR_API_TOKEN=...
  - appsettings.json: adicione 'Extrator': { 'Url': '...', 'ApiToken': '...' }
";

        _logger?.LogError(errorMessage);
        return Result.Failure<(string url, string token)>(new Error(
            "ExtractorConfigNotFound",
            errorMessage.Trim()));
    }

    public string GetConfigSource() => _lastSource;

    // ============ PRIVATE METHODS ============

    /// <summary>
    /// Tenta carregar diretamente do AWS Secrets Manager (tools/diax-extrator).
    /// Se falhar (timeout, 403, offline), retorna null para continuar cascata.
    /// </summary>
    private async Task<Result<(string url, string token)>?> TryLoadFromAwsSecretsManagerAsync()
    {
        try
        {
            var client = new AmazonSecretsManagerClient(RegionEndpoint.USEast1);
            var request = new GetSecretValueRequest { SecretId = "tools/diax-extrator" };
            var response = await client.GetSecretValueAsync(request);

            if (string.IsNullOrEmpty(response.SecretString))
            {
                _logger?.LogWarning("⚠️ AWS SM secret 'tools/diax-extrator' is empty");
                return null; // Continue para próxima camada
            }

            var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            var url = secret?["EXTRATOR_URL"] ?? secret?["extractorUrl"];
            var token = secret?["EXTRATOR_API_TOKEN"] ?? secret?["extractorToken"];

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
            {
                _logger?.LogWarning("⚠️ AWS SM secret 'tools/diax-extrator' missing required keys (EXTRATOR_URL, EXTRATOR_API_TOKEN)");
                return null; // Continue para próxima camada
            }

            return Result.Success<(string, string)>((url, token));
        }
        catch (ResourceNotFoundException)
        {
            _logger?.LogWarning("⚠️ AWS SM secret 'tools/diax-extrator' not found");
            return null; // Continue para próxima camada
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "⚠️ AWS Secrets Manager unavailable ({Message}), falling back to configuration cascade", ex.Message);
            return null; // Continue para próxima camada
        }
    }

    /// <summary>
    /// Tenta carregar de IConfiguration.
    /// Automaticamente cobre:
    /// - Environment Variables
    /// - appsettings.json
    /// - appsettings.{Environment}.json
    /// - User Secrets
    /// - Web.config (IIS)
    /// - CI/CD Secrets (via env vars)
    /// </summary>
    private Result<(string url, string token)> TryLoadFromConfiguration()
    {
        var url = _configuration["Extrator:Url"]
            ?? _configuration["DIAX_EXTRATOR_URL"];

        var token = _configuration["Extrator:ApiToken"]
            ?? _configuration["DIAX_EXTRATOR_API_TOKEN"];

        if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(token))
        {
            return Result.Success<(string, string)>((url, token));
        }

        return Result.Failure<(string url, string token)>(new Error(
            "ConfigurationNotFound",
            "Configuration not found in IConfiguration sources"));
    }

    /// <summary>
    /// Determina de qual fonte específica veio a config.
    /// Útil para logging e troubleshooting.
    /// </summary>
    private string DetermineConfigurationSource()
    {
        // Ordem de verificação (mesma cascata)
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DIAX_EXTRATOR_URL")))
        {
            return "Environment Variables";
        }

        if (!string.IsNullOrWhiteSpace(_configuration["Extrator:Url"]))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            return $"appsettings.{env}.json / appsettings.json";
        }

        return "IConfiguration (User Secrets / Web.config)";
    }

}
