using Diax.Domain.Common;
using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Outreach;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Application.Customers;

public class ApifyIntegrationService : IApifyIntegrationService
{
    private readonly IOutreachConfigRepository _configRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CustomerImportService _customerImportService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApifyIntegrationService> _logger;

    public ApifyIntegrationService(
        IOutreachConfigRepository configRepository,
        ICurrentUserService currentUserService,
        CustomerImportService customerImportService,
        IHttpClientFactory httpClientFactory,
        ILogger<ApifyIntegrationService> logger)
    {
        _configRepository = configRepository;
        _currentUserService = currentUserService;
        _customerImportService = customerImportService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<Guid>> ImportDatasetAsync(string datasetUrl, int source, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result.Failure<Guid>(Error.Unauthorized());

        var config = await _configRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.ApifyApiToken))
        {
            return Result.Failure<Guid>(Error.Validation("Apify.NoToken", "Token da API da Apify não configurado no Outreach."));
        }

        try
        {
            // Validar que a URL pertence ao domínio oficial da Apify (prevenção de SSRF)
            var rawUrl = datasetUrl.Trim();
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsedUri)
                || parsedUri.Scheme != "https"
                || !parsedUri.Host.Equals("api.apify.com", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<Guid>(Error.Validation("Apify.InvalidUrl",
                    "A URL do dataset deve ser do domínio https://api.apify.com."));
            }

            // Adicionar token como query param (sem sobrescrever se já vier na URL)
            var uriBuilder = new UriBuilder(parsedUri);
            var existingQuery = uriBuilder.Query.TrimStart('?');
            var hasToken = existingQuery.Split('&')
                .Any(p => p.StartsWith("token=", StringComparison.OrdinalIgnoreCase));
            if (!hasToken)
            {
                var sep = string.IsNullOrEmpty(existingQuery) ? "" : "&";
                uriBuilder.Query = existingQuery + sep + "token=" + Uri.EscapeDataString(config.ApifyApiToken);
            }
            var url = uriBuilder.Uri.ToString();

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5); // datasets can be large

            _logger.LogInformation("Fetching Apify dataset from {Url}", parsedUri.GetLeftPart(UriPartial.Path));

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Falha ao buscar dataset da Apify: {StatusCode} {Error}", response.StatusCode, errorContent);
                return Result.Failure<Guid>(new Error("Apify.FetchError", $"Erro ao consultar dataset: {response.StatusCode}"));
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse generic Json array
            using var document = JsonDocument.Parse(jsonContent);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Result.Failure<Guid>(Error.Validation("Apify.InvalidFormat", "O retorno da Apify deve ser um array JSON."));
            }

            var customersToImport = new List<ImportCustomerRow>();

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var row = MapToCustomerRow(item);
                if (row != null)
                {
                    customersToImport.Add(row);
                }
            }

            if (customersToImport.Count == 0)
            {
                return Result.Failure<Guid>(Error.Validation("Apify.NoData", "Nenhum item válido encontrado no dataset para importar."));
            }

            // Repass to internal import service
            var importRequest = new BulkImportRequest(
                Customers: customersToImport,
                Source: (LeadSource)source
            );

            var fileName = $"Apify Dataset - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            var result = await _customerImportService.ImportAsync(importRequest, fileName, cancellationToken);

            if (!result.Success)
            {
                return Result.Failure<Guid>(new Error("Apify.ImportError", $"Importação concluída com erros: {result.FailedCount} falharam."));
            }

            return Result.Success(Guid.NewGuid()); // The ImportService does not return an ID today, returning a fake Guid just for Result status if success
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento da Apify.");
            return Result.Failure<Guid>(new Error("Apify.Exception", $"Erro interno: {ex.Message}"));
        }
    }

    private ImportCustomerRow? MapToCustomerRow(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;

        var name = GetStringValue(item, "title", "name", "companyName", "businessName", "text");
        if (string.IsNullOrWhiteSpace(name)) return null; // Name is required in CRM

        var phoneStr = GetStringValue(item, "phone", "phoneNumber", "phoneUnformatted");
        var whatsappStr = GetStringValue(item, "whatsapp") ?? phoneStr;

        var emailStr = string.Empty;
        var emails = GetStringArrayValue(item, "emails");
        if (emails.Count > 0)
        {
            emailStr = emails[0];
        }
        else
        {
            emailStr = GetStringValue(item, "email", "contactEmail") ?? string.Empty;
        }

        // Collect extra info as notes
        var noteParts = new List<string>();

        var website = GetStringValue(item, "website", "url", "domain");
        if (!string.IsNullOrWhiteSpace(website)) noteParts.Add($"Website: {website}");

        var city = GetStringValue(item, "city");
        if (!string.IsNullOrWhiteSpace(city)) noteParts.Add($"Cidade: {city}");

        var score = GetStringValue(item, "totalScore", "rating", "stars");
        if (!string.IsNullOrWhiteSpace(score)) noteParts.Add($"Nota/Avaliação: {score}");

        var reviews = GetStringValue(item, "reviewsCount", "numberOfReviews");
        if (!string.IsNullOrWhiteSpace(reviews)) noteParts.Add($"Qtd Avaliações: {reviews}");

        AddSocialToNotes(noteParts, item, "instagrams", "Instagram");
        AddSocialToNotes(noteParts, item, "facebooks", "Facebook");
        AddSocialToNotes(noteParts, item, "linkedIns", "LinkedIn");
        AddSocialToNotes(noteParts, item, "tiktoks", "TikTok");
        AddSocialToNotes(noteParts, item, "twitters", "Twitter");

        // Tags
        var tags = new List<string> { "apify-import" };
        if (!string.IsNullOrWhiteSpace(city))
            tags.Add(city.ToLowerInvariant().Replace(" ", "-"));

        var row = new ImportCustomerRow(
            Name: name,
            Email: emailStr,
            Phone: phoneStr,
            WhatsApp: whatsappStr,
            CompanyName: name,
            Notes: noteParts.Count > 0 ? string.Join("\n", noteParts) : null,
            Tags: string.Join(",", tags)
        );

        return row;
    }

    private string? GetStringValue(JsonElement item, params string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (item.TryGetProperty(prop, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    var str = value.GetString();
                    if (!string.IsNullOrWhiteSpace(str)) return str;
                }
                else if (value.ValueKind == JsonValueKind.Number)
                {
                    return value.GetRawText();
                }
            }
        }
        return null;
    }

    private List<string> GetStringArrayValue(JsonElement item, string propertyName)
    {
        var list = new List<string>();
        if (item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var str = element.GetString();
                    if (!string.IsNullOrWhiteSpace(str)) list.Add(str);
                }
            }
        }
        return list;
    }

    private void AddSocialToNotes(List<string> notes, JsonElement item, string propName, string label)
    {
        var links = GetStringArrayValue(item, propName);
        if (links.Count > 0)
        {
            notes.Add($"{label}: {links[0]}");
        }
    }
}
