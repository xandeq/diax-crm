using System.Text.Json;
using Diax.Application.AI;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.LeadPersona;

public class LeadPersonaGeneratorService : IApplicationService, ILeadPersonaGeneratorService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _modelValidator;
    private readonly IAiUsageTrackingService _usageTracker;
    private readonly IAiProviderRepository _providerRepo;
    private readonly IAiModelRepository _modelRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ILogger<LeadPersonaGeneratorService> _logger;

    public LeadPersonaGeneratorService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator modelValidator,
        IAiUsageTrackingService usageTracker,
        IAiProviderRepository providerRepo,
        IAiModelRepository modelRepo,
        ICustomerRepository customerRepo,
        ILogger<LeadPersonaGeneratorService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _modelValidator = modelValidator;
        _usageTracker = usageTracker;
        _providerRepo = providerRepo;
        _modelRepo = modelRepo;
        _customerRepo = customerRepo;
        _logger = logger;
    }

    public async Task<GeneratePersonasResponseDto> GeneratePersonasAsync(
        GeneratePersonasRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new ArgumentException("Provider is required");

        var count = request.Count ?? 5;
        if (count < 1 || count > 10)
            count = 5;

        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("POST /api/v1/ai/lead-personas - Request received | RequestId: {RequestId}", requestId);

        // Fetch leads for analysis
        var leads = (await _customerRepo.FindAsync(
            c => c.Status < CustomerStatus.Customer, ct))
            .Take(200) // Limit to 200 leads for analysis
            .ToList();

        if (leads.Count == 0)
            throw new InvalidOperationException("No leads found to analyze");

        // Build lead summary for AI
        var leadSummary = new System.Text.StringBuilder();
        leadSummary.AppendLine("Analyze these leads and generate buyer personas:\n");

        foreach (var lead in leads.Take(50))
        {
            leadSummary.AppendLine($"- Name: {lead.Name}");
            if (!string.IsNullOrWhiteSpace(lead.CompanyName))
                leadSummary.AppendLine($"  Company: {lead.CompanyName}");
            if (!string.IsNullOrWhiteSpace(lead.Notes))
                leadSummary.AppendLine($"  Notes: {lead.Notes}");
            if (!string.IsNullOrWhiteSpace(lead.Tags))
                leadSummary.AppendLine($"  Tags: {lead.Tags}");
            leadSummary.AppendLine();
        }

        // Validate provider
        var providerKey = request.Provider.ToLower();
        var provider = await _providerRepo.GetByKeyAsync(providerKey, ct);
        if (provider == null)
            throw new InvalidOperationException($"Provider {request.Provider} not found");

        // Get AI client
        var client = _aiClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"AI client for {request.Provider} not available");

        // Get provider configuration
        var providerConfig = _settings.GetProviderConfig(providerKey)
            ?? throw new InvalidOperationException(
                $"Configuration not found for provider {request.Provider}. " +
                "Verify that appsettings contains the PromptGenerator section with provider credentials.");

        if (string.IsNullOrWhiteSpace(providerConfig.ApiKey))
            throw new InvalidOperationException($"API key not configured for provider {request.Provider}");

        // Build system prompt
        var systemPrompt = @"You are a buyer persona expert. Analyze the leads provided and generate detailed buyer personas.
Each persona should represent a distinct group of similar prospects.
Return a valid JSON array with personas. Include: name, title, companyType, industry, painPoints (array), goals (array),
budgetRange, decisionProcess, effectiveChannels (array), outreachMessages (array), leadExamples (array).";

        // Build user prompt
        var userPrompt = $@"{leadSummary}

Generate {count} detailed buyer personas from these {leads.Count} leads.
{(request.FocusSegment != null ? $"Focus on: {request.FocusSegment}" : "")}
{(request.IncludeOutreachTips == true ? "Include tailored outreach message angles for each persona." : "")}

Return valid JSON array only, no markdown or extra text.";

        // Call AI
        var options = new AiClientOptions(
            ApiKey: providerConfig.ApiKey,
            BaseUrl: providerConfig.BaseUrl ?? string.Empty,
            Model: !string.IsNullOrWhiteSpace(request.Model) ? request.Model : (providerConfig.Model ?? string.Empty),
            Temperature: request.Temperature ?? 0.7,
            MaxTokens: request.MaxTokens ?? 2000
        );

        _logger.LogInformation("Calling {Provider} for persona generation", request.Provider);
        var response = await client.TransformAsync(systemPrompt, userPrompt, options, ct);

        // Parse response
        var responseText = response.Trim();
        if (responseText.StartsWith("```json"))
            responseText = responseText[7..];
        if (responseText.StartsWith("```"))
            responseText = responseText[3..];
        if (responseText.EndsWith("```"))
            responseText = responseText[..^3];

        responseText = responseText.Trim();

        var personas = JsonSerializer.Deserialize<PersonaDto[]>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var completionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Fire-and-forget usage tracking
        var modelId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            var model = await _modelRepo.GetByProviderAndModelKeyAsync(provider.Id, request.Model, ct);
            if (model != null)
                modelId = model.Id;
        }

        _ = _usageTracker.LogUsageAsync(
            userId: userId,
            providerId: provider.Id,
            modelId: modelId,
            featureType: "LeadPersonaGenerator",
            duration: DateTime.UtcNow - startTime,
            success: personas.Length > 0,
            requestId: requestId,
            cancellationToken: ct);

        var result = new GeneratePersonasResponseDto
        {
            Personas = personas,
            RequestId = requestId,
            ProviderUsed = request.Provider,
            ModelUsed = request.Model,
            CompletionTime = completionTime,
            LeadsAnalyzed = leads.Count
        };

        _logger.LogInformation("Lead persona generation completed | RequestId: {RequestId} | Personas: {Count}",
            requestId, personas.Length);

        return result;
    }
}
