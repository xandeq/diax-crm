using System.Text.Json;
using Diax.Shared.Interfaces;
using Diax.Shared.Results;
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
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(
        IConfigurationProvider configProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ExtractorService> logger)
    {
        _configProvider = configProvider;
        _httpClientFactory = httpClientFactory;
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
            if (configResult.IsFailed)
            {
                _logger.LogError("Failed to load Extrator config: {Error}",
                    configResult.FirstError.Description);
                return Result.Failure(configResult.FirstError);
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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Extrator API error: {StatusCode} - {Content}",
                    response.StatusCode,
                    errorContent);

                return Result.Failure(new Error(
                    code: "ExtractorApiError",
                    description: $"Extrator retornou erro {response.StatusCode}. Verifique a configuração e tente novamente."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var leadsResponse = JsonSerializer.Deserialize<ExtractorLeadsResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (leadsResponse == null)
            {
                _logger.LogWarning("Extrator returned null response");
                return Result.Failure(new Error(
                    code: "ExtractorInvalidResponse",
                    description: "Resposta inválida do Extrator"));
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
            return Result.Failure(new Error(
                code: "ExtractorConnectionError",
                description: $"Erro ao conectar com Extrator: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching from Extrator");
            return Result.Failure(new Error(
                code: "ExtractorError",
                description: $"Erro inesperado: {ex.Message}"));
        }
    }
}

/// <summary>
/// Response from Extrator API /api/leads endpoint
/// </summary>
public class ExtractorLeadsResponse
{
    public List<ExtractorLead>? Leads { get; set; }
    public int? Total { get; set; }
    public int? Page { get; set; }
    public int? PerPage { get; set; }
}

/// <summary>
/// Lead object from Extrator API
/// </summary>
public class ExtractorLead
{
    public long Id { get; set; }
    public string? ContactName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? CrmStatus { get; set; }
    public string? Tags { get; set; }
    public string? Website { get; set; }
}
