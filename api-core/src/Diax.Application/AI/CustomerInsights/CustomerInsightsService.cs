using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Application.AI.CustomerInsights;

public class CustomerInsightsService : IApplicationService, ICustomerInsightsService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<CustomerInsightsService> _logger;

    public CustomerInsightsService(
        ICustomerRepository customerRepository,
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ILogger<CustomerInsightsService> logger)
    {
        _customerRepository = customerRepository;
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<GenerateInsightsResponseDto> GenerateInsightsAsync(
        GenerateInsightsRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        var providerKey = request.Provider.ToLower();

        var isValidProvider = await _aiModelValidator.IsValidProviderAsync(providerKey, ct);
        if (!isValidProvider)
        {
            var activeProviders = await _aiModelValidator.GetActiveProviderKeysAsync(ct);
            throw new ArgumentException(
                $"Provedor '{request.Provider}' não está ativo. Disponíveis: {string.Join(", ", activeProviders)}");
        }

        var providerSettings = _settings.GetProviderConfig(providerKey)
            ?? throw new InvalidOperationException($"Configuração não encontrada para '{providerKey}'.");
        if (string.IsNullOrWhiteSpace(providerSettings.ApiKey))
            throw new InvalidOperationException($"API Key não configurada para '{providerKey}'.");

        var client = _aiClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Client não encontrado para '{providerKey}'.");

        var options = new AiClientOptions(
            ApiKey: providerSettings.ApiKey,
            BaseUrl: providerSettings.BaseUrl ?? string.Empty,
            Model: !string.IsNullOrWhiteSpace(request.Model) ? request.Model : (providerSettings.Model ?? string.Empty),
            Temperature: request.Temperature ?? 0.5,
            MaxTokens: request.MaxTokens ?? 3000
        );

        // Gather real data from the database
        var allCustomers = await _customerRepository.GetAllAsync(ct);
        var customersList = allCustomers.ToList();

        var daysBack = request.DateRange switch
        {
            "last_7_days" => 7,
            "last_30_days" => 30,
            "last_90_days" => 90,
            "last_year" => 365,
            _ => 30
        };

        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

        var totalLeads = customersList.Count(c => c.IsLead);
        var totalCustomers = customersList.Count(c => c.IsActiveCustomer);
        var newInPeriod = customersList.Count(c => c.CreatedAt >= cutoffDate);
        var convertedInPeriod = customersList.Count(c => c.ConvertedAt != null && c.ConvertedAt >= cutoffDate);
        var conversionRate = totalLeads + totalCustomers > 0
            ? Math.Round((decimal)totalCustomers / (totalLeads + totalCustomers) * 100, 1)
            : 0;

        // Source distribution
        var sourceGroups = customersList
            .GroupBy(c => c.Source)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        var topSource = sourceGroups.FirstOrDefault() ?? "N/A";

        // Status distribution
        var statusGroups = customersList
            .GroupBy(c => c.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        // Segment distribution
        var segmentGroups = customersList
            .Where(c => c.IsLead)
            .GroupBy(c => c.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        var topSegment = segmentGroups.FirstOrDefault() ?? "N/A";

        // Tags analysis
        var tagCounts = customersList
            .Where(c => !string.IsNullOrWhiteSpace(c.Tags))
            .SelectMany(c => c.Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        var summary = new InsightsSummaryDto(
            TotalLeads: totalLeads,
            TotalCustomers: totalCustomers,
            NewLeadsInPeriod: newInPeriod,
            ConvertedInPeriod: convertedInPeriod,
            ConversionRate: conversionRate,
            TopSource: topSource,
            TopSegment: topSegment
        );

        var systemPrompt = @"Você é um analista de CRM e marketing digital experiente. Analise os dados de leads/clientes fornecidos e identifique padrões, oportunidades e recomendações acionáveis. Retorne como JSON puro.";

        var focusContext = !string.IsNullOrWhiteSpace(request.FocusArea)
            ? $"Foco da análise: {request.FocusArea}\n" : "";

        var userPrompt = $@"Dados do CRM (últimos {daysBack} dias):

Total de leads: {totalLeads}
Total de clientes convertidos: {totalCustomers}
Novos leads no período: {newInPeriod}
Convertidos no período: {convertedInPeriod}
Taxa de conversão: {conversionRate}%

Distribuição por origem: {string.Join(", ", sourceGroups)}
Distribuição por status: {string.Join(", ", statusGroups)}
Top tags: {(tagCounts.Any() ? string.Join(", ", tagCounts) : "nenhuma tag")}
{focusContext}
Requisitos:
- Identifique 3-5 padrões nos dados (tendências, oportunidades, riscos)
- Para cada padrão: title (string), description (2-3 frases), impact (""alto"", ""médio"", ""baixo""), category (""conversão"", ""segmentação"", ""engajamento"", ""qualidade"", ""oportunidade"")
- Gere 3-5 recomendações acionáveis e específicas
- Título do relatório deve ser descritivo

Formato JSON:
{{
  ""title"": ""Análise de Leads — Abril 2026"",
  ""patterns"": [
    {{""title"": ""..."", ""description"": ""..."", ""impact"": ""alto"", ""category"": ""conversão""}}
  ],
  ""recommendations"": [""Ação 1..."", ""Ação 2...""]
}}

Gere a análise:";

        var requestId = Guid.NewGuid().ToString();
        _logger.LogInformation("Customer insights started. RequestId: {RequestId}. Provider: {Provider}.",
            requestId, providerKey);

        var startTime = DateTime.UtcNow;

        try
        {
            var response = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Customer insights completed. RequestId: {RequestId}. Duration: {Duration}ms",
                requestId, duration.TotalMilliseconds);

            var (title, patterns, recommendations) = ParseInsightsFromResponse(response);

            TrackUsageFireAndForget(userId, providerKey, request.Model, requestId, duration, true, null);

            return new GenerateInsightsResponseDto(
                Title: title,
                Summary: summary,
                Patterns: patterns,
                Recommendations: recommendations,
                ProviderUsed: providerKey,
                ModelUsed: options.Model,
                GeneratedAt: DateTime.UtcNow,
                RequestId: requestId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            TrackUsageFireAndForget(userId, providerKey, request.Model, requestId, duration, false, ex.Message);
            throw;
        }
    }

    private void TrackUsageFireAndForget(Guid userId, string providerKey, string? modelKey, string requestId,
        TimeSpan duration, bool success, string? errorMessage)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var provider = await _providerRepository.GetByKeyAsync(providerKey, CancellationToken.None);
                if (provider != null)
                {
                    var model = modelKey != null
                        ? await _modelRepository.GetByProviderAndModelKeyAsync(provider.Id, modelKey, CancellationToken.None)
                        : (await _modelRepository.GetByProviderIdAsync(provider.Id, CancellationToken.None)).FirstOrDefault();

                    if (model != null)
                    {
                        await _usageTracking.LogUsageAsync(
                            userId: userId,
                            providerId: provider.Id,
                            modelId: model.Id,
                            featureType: "CustomerInsights",
                            duration: duration,
                            success: success,
                            requestId: requestId,
                            errorMessage: errorMessage,
                            cancellationToken: CancellationToken.None
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track usage for request {RequestId}", requestId);
            }
        }, CancellationToken.None);
    }

    private (string title, List<InsightPatternDto> patterns, List<string> recommendations) ParseInsightsFromResponse(string response)
    {
        var title = "Relatório de Insights";
        var patterns = new List<InsightPatternDto>();
        var recommendations = new List<string>();

        try
        {
            var jsonContent = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("title", out var titleEl))
                title = titleEl.GetString() ?? title;

            if (root.TryGetProperty("patterns", out var patternsArray) && patternsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in patternsArray.EnumerateArray())
                {
                    var pTitle = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                    var impact = item.TryGetProperty("impact", out var i) ? i.GetString() ?? "médio" : "médio";
                    var category = item.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "";

                    if (!string.IsNullOrWhiteSpace(pTitle))
                        patterns.Add(new InsightPatternDto(pTitle, desc, impact, category));
                }
            }

            if (root.TryGetProperty("recommendations", out var recsArray) && recsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in recsArray.EnumerateArray())
                {
                    var rec = item.GetString();
                    if (!string.IsNullOrWhiteSpace(rec))
                        recommendations.Add(rec);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON parsing error in insights response: {Error}", ex.Message);
        }

        if (patterns.Count == 0)
            patterns.Add(new InsightPatternDto("Erro na análise", "Não foi possível gerar insights.", "baixo", "erro"));

        return (title, patterns, recommendations);
    }
}
