using Diax.Shared.Interfaces;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    /// Carrega configuração do Extrator em cascata simplificada (SaaS-grade).
    ///
    /// Cascata (ordem de prioridade):
    /// 1. Environment Variables (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN)
    /// 2. appsettings.{Environment}.json (Extrator:Url, Extrator:ApiToken)
    /// 3. appsettings.json (Extrator:Url, Extrator:ApiToken)
    /// 4. .NET User Secrets (desenvolvimento)
    ///
    /// Notas:
    /// - DotNetEnv carrega .env automaticamente → Environment Variables
    /// - Web.config (IIS) injeta em IConfiguration automaticamente
    /// - CI/CD Secrets chegam como Environment Variables
    /// - Resultado é cacheado por 5 minutos para performance
    /// </summary>
    public async Task<Result<(string url, string token)>> GetExtractorConfigAsync()
    {
        // ✅ Check cache first
        if (_cache.TryGetValue(CACHE_KEY, out var cached))
        {
            var (cachedUrl, cachedToken, cachedSource) = ((string, string, string))cached;
            _lastSource = cachedSource;
            _logger.LogDebug("✓ Extrator config loaded from CACHE (source: {Source})", _lastSource);
            return Result.Success<(string, string)>((cachedUrl, cachedToken));
        }

        // 1️⃣ IConfiguration (Environment Variables + appsettings + User Secrets)
        var result = TryLoadFromConfiguration();
        if (result.IsSuccess)
        {
            var (url, token) = result.Value;
            _lastSource = DetermineConfigurationSource();

            // ✅ Cache the result
            _cache.Set(CACHE_KEY, (url, token, _lastSource), CACHE_DURATION);
            _logger.LogInformation("✓ Extrator config loaded from {Source}", _lastSource);
            return result;
        }

        // ❌ Nenhuma fonte disponível
        _lastSource = "NOT_FOUND";
        var errorMessage = @"
❌ Extrator configuration not found.

Cascata testada (em ordem):
  1. Environment Variables (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN)
  2. appsettings.{Environment}.json (Extrator:Url, Extrator:ApiToken)
  3. appsettings.json (Extrator:Url, Extrator:ApiToken)
  4. .NET User Secrets (dotnet user-secrets set ...)

Configure em pelo menos UMA dessas fontes.";

        _logger.LogError(errorMessage);
        return Result.Failure<(string url, string token)>(new Error(
            "ExtractorConfigNotFound",
            errorMessage.Trim()));
    }

    public string GetConfigSource() => _lastSource;

    // ============ PRIVATE METHODS ============

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
