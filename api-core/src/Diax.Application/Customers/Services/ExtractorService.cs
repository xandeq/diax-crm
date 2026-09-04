using System.Text.Json;
using System.Text.Json.Serialization;
using Diax.Shared.Interfaces;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers.Services;

public interface IExtractorService
{
    Task<Result<ExtractorLeadsResponse>> FetchLeadsAsync(
        string? search = null,
        string? status = null,
        string? tag = null,
        string? city = null,
        int page = 1,
        int perPage = 100);
}

public class ExtractorService : IExtractorService
{
    private readonly IConfigurationProvider _configProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(
        IConfigurationProvider configProvider,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ExtractorService> logger)
    {
        _configProvider = configProvider;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Busca leads do Extrator (server-to-server).
    /// Token é mantido seguro no backend, nunca exposto ao frontend.
    /// </summary>
    public async Task<Result<ExtractorLeadsResponse>> FetchLeadsAsync(
        string? search = null,
        string? status = null,
        string? tag = null,
        string? city = null,
        int page = 1,
        int perPage = 100)
    {
        try
        {
            // ✅ Fetch config from cascade (secure)
            var configResult = await _configProvider.GetExtractorConfigAsync();
            if (configResult.IsFailure)
            {
                _logger.LogError("Failed to load Extrator config: {Error}",
                    configResult.Error.Message);
                return Result.Failure<ExtractorLeadsResponse>(configResult.Error);
            }

            var (extractorUrl, extractorToken) = configResult.Value;

            // Build query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "per_page", perPage.ToString() },
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams["search"] = search;
            if (!string.IsNullOrWhiteSpace(status))
                queryParams["status"] = status;
            if (!string.IsNullOrWhiteSpace(tag))
                queryParams["tag"] = tag;
            if (!string.IsNullOrWhiteSpace(city))
                queryParams["city"] = city;

            var queryString = string.Join("&",
                queryParams.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

            var url = $"{extractorUrl.TrimEnd('/')}/api/leads?{queryString}";

            // ✅ Make server-to-server request
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {extractorToken}");
            client.Timeout = TimeSpan.FromSeconds(30);

            _logger.LogInformation("Fetching leads from Extrator: {Url} (filters: search={Search}, status={Status}, tag={Tag}, city={City})",
                url, search ?? "null", status ?? "null", tag ?? "null", city ?? "null");

            var response = await client.GetAsync(url);

            // 403 Forbidden = credencial de serviço reconhecida mas SEM escopo/permissão.
            // Isso não é resolvido recarregando o token → NÃO fazer retry.
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Extrator rejeitou a credencial de serviço com 403 Forbidden (escopo/permissão insuficiente). Sem retry.");

                return Result.Failure<ExtractorLeadsResponse>(new Error(
                    "ExtractorServiceForbidden",
                    "O Extrator recusou a credencial de serviço (403 Forbidden). A credencial foi reconhecida mas " +
                    "não possui o escopo/permissão necessário. Verifique se EXTRATOR_SERVICE_TOKEN corresponde entre " +
                    "o segredo do CRM (tools/diax-extrator) e o cofre de segredos do Extrator (extratordedados/prod)."));
            }

            // 401 Unauthorized = token pode ter rotacionado no AWS Secrets Manager.
            // Invalida cache, recarrega config UMA vez e tenta novamente UMA vez.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(
                    "Extrator retornou 401 Unauthorized. Invalidando cache de config e tentando novamente uma única vez...");

                // Limpar cache para forçar novo fetch do ConfigurationProvider (pega possível token rotacionado)
                _cache.Remove("extractor_config");

                // Retry único com token "fresco" do cascade. Token nunca é logado.
                var freshConfigResult = await _configProvider.GetExtractorConfigAsync();
                if (freshConfigResult.IsSuccess)
                {
                    var (freshUrl, freshToken) = freshConfigResult.Value;
                    // Reconstrói a URL a partir da base recarregada: a rotação/migração pode
                    // trocar o HOST, não só o token. Reusar a `url` antiga mandaria a credencial
                    // nova para o host obsoleto (recuperação falha + risco de exposição).
                    var retryUrl = $"{freshUrl.TrimEnd('/')}/api/leads?{queryString}";
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {freshToken}");

                    response = await client.GetAsync(retryUrl);
                }
                else
                {
                    _logger.LogError(
                        "Falha ao recarregar config do Extrator durante retry de 401: {Error}",
                        freshConfigResult.Error.Message);
                }

                // 403 no retry (ex.: credencial recarregada sem escopo) — trata como o 403 inicial, sem novo retry.
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError(
                        "Extrator retornou 403 Forbidden no retry pós-401 (escopo/permissão insuficiente). Sem novo retry.");

                    return Result.Failure<ExtractorLeadsResponse>(new Error(
                        "ExtractorServiceForbidden",
                        "O Extrator recusou a credencial de serviço (403 Forbidden) no retry. A credencial foi reconhecida mas " +
                        "não possui o escopo/permissão necessário. Verifique se EXTRATOR_SERVICE_TOKEN corresponde entre " +
                        "o segredo do CRM (tools/diax-extrator) e o cofre de segredos do Extrator (extratordedados/prod)."));
                }

                // Se AINDA está 401 após o retry (ou a config não recarregou), a credencial está de fato rejeitada.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError(
                        "Extrator continuou retornando 401 Unauthorized após um retry com config recarregada. Credencial de serviço rejeitada.");

                    return Result.Failure<ExtractorLeadsResponse>(new Error(
                        "ExtractorServiceUnauthorized",
                        "O Extrator recusou a credencial de serviço (401 Unauthorized) mesmo após recarregar a configuração. " +
                        "Verifique se EXTRATOR_SERVICE_TOKEN corresponde entre o segredo do CRM (tools/diax-extrator) " +
                        "e o cofre de segredos do Extrator (extratordedados/prod)."));
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Extrator API error: {StatusCode} - {Content}",
                    response.StatusCode,
                    errorContent);

                return Result.Failure<ExtractorLeadsResponse>(new Error(
                    "ExtractorApiError",
                    $"Extrator retornou erro {response.StatusCode}. Verifique a configuração e tente novamente."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var leadsResponse = JsonSerializer.Deserialize<ExtractorLeadsResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (leadsResponse == null)
            {
                _logger.LogWarning("Extrator returned null response");
                return Result.Failure<ExtractorLeadsResponse>(new Error(
                    "ExtractorInvalidResponse",
                    "Resposta inválida do Extrator"));
            }

            _logger.LogInformation(
                "Successfully fetched {Count} leads from Extrator (page: {Page}, total: {Total})",
                leadsResponse.Leads?.Count ?? 0,
                leadsResponse.Page ?? 1,
                leadsResponse.Total ?? 0);

            return Result.Success(leadsResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error connecting to Extrator");
            return Result.Failure<ExtractorLeadsResponse>(new Error(
                "ExtractorConnectionError",
                $"Erro ao conectar com Extrator: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching from Extrator");
            return Result.Failure<ExtractorLeadsResponse>(new Error(
                "ExtractorError",
                $"Erro inesperado: {ex.Message}"));
        }
    }
}

/// <summary>
/// Response from Extrator API /api/leads endpoint
/// </summary>
public class ExtractorLeadsResponse
{
    [JsonPropertyName("leads")]
    public List<ExtractorLead>? Leads { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }
}

/// <summary>
/// Lead object from Extrator API.
/// The Extrator serializes fields in snake_case (Flask jsonify), so each
/// multi-word field is mapped explicitly — PropertyNameCaseInsensitive alone
/// does NOT strip underscores, which previously bound contact_name/company_name/
/// crm_status to null and silently dropped every pulled lead.
/// </summary>
public class ExtractorLead
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("contact_name")]
    public string? ContactName { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("whatsapp")]
    public string? WhatsApp { get; set; }

    [JsonPropertyName("crm_status")]
    public string? CrmStatus { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }
}
