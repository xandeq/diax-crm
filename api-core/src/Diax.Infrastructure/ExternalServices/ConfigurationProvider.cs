using Diax.Infrastructure.Data;
using Diax.Shared.Interfaces;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.ExternalServices;

public class ConfigurationProvider : IConfigurationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationProvider> _logger;
    private readonly DiaxDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private string _lastSource = "";

    private const string CACHE_KEY = "extractor_config";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    public ConfigurationProvider(
        IConfiguration configuration,
        ILogger<ConfigurationProvider> logger,
        DiaxDbContext dbContext,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <summary>
    /// Carrega configuração do Extrator em cascata simplificada (SaaS-grade).
    ///
    /// Cascata (ordem de prioridade):
    /// 1. Environment Variables (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN)
    /// 2. appsettings.{Environment}.json
    /// 3. appsettings.json
    /// 4. .NET User Secrets (desenvolvimento)
    /// 5. Database Config table (runtime alterável)
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
            return Result.Success((cachedUrl, cachedToken));
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

        // 2️⃣ Database Config table (último recurso, runtime alterável)
        var dbResult = await TryLoadFromDatabaseAsync();
        if (dbResult.IsSuccess)
        {
            var (url, token) = dbResult.Value;
            _lastSource = "Database (ApplicationConfig)";

            // ✅ Cache the result
            _cache.Set(CACHE_KEY, (url, token, _lastSource), CACHE_DURATION);
            _logger.LogInformation("✓ Extrator config loaded from {Source}", _lastSource);
            return dbResult;
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
  5. Database (ApplicationConfig table)

Configure em pelo menos UMA dessas fontes.";

        _logger.LogError(errorMessage);
        return Result.Failure(new Error(
            code: "ExtractorConfigNotFound",
            description: errorMessage.Trim()));
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
            return Result.Success((url, token));
        }

        return Result.Failure(new Error("ConfigurationNotFound"));
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

    /// <summary>
    /// Carrega do banco de dados como último recurso.
    /// Permite alterar config em runtime sem redeploy.
    /// </summary>
    private async Task<Result<(string url, string token)>> TryLoadFromDatabaseAsync()
    {
        try
        {
            // Busca ambas as configs da DB
            var configs = await _dbContext.ApplicationConfigs
                .AsNoTracking()
                .Where(x => (x.Key == "ExtractorUrl" || x.Key == "ExtractorApiToken")
                    && x.IsActive
                    && (x.Environment == null || x.Environment == GetCurrentEnvironment()))
                .ToListAsync();

            var urlConfig = configs.FirstOrDefault(x => x.Key == "ExtractorUrl");
            var tokenConfig = configs.FirstOrDefault(x => x.Key == "ExtractorApiToken");

            if (urlConfig != null && tokenConfig != null
                && !string.IsNullOrWhiteSpace(urlConfig.Value)
                && !string.IsNullOrWhiteSpace(tokenConfig.Value))
            {
                _logger.LogInformation(
                    "Config loaded from DB. LastUpdated: {UrlUpdated} (URL), {TokenUpdated} (Token)",
                    urlConfig.UpdatedAt, tokenConfig.UpdatedAt);

                return Result.Success((urlConfig.Value, tokenConfig.Value));
            }

            return Result.Failure(new Error("ConfigNotInDatabase"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading config from database");
            return Result.Failure(new Error("DatabaseError", ex.Message));
        }
    }

    private string GetCurrentEnvironment() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
}
