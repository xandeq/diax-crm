using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Application.PromptGenerator;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Application.EmailMarketing.Pro;

public class LeadNormalizationService : ILeadNormalizationService
{
    private static readonly (string ProviderKey, string Model)[] AiProviderPreference =
    [
        ("anthropic",  "claude-haiku-4-5-20251001"),
        ("openai",     "gpt-4o-mini"),
        ("groq",       "llama-3.1-8b-instant"),
        ("openrouter", "meta-llama/llama-3.1-8b-instruct:free"),
    ];

    private readonly ICustomerRepository _repo;
    private readonly INameNormalizationService _normalizer;
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly ILogger<LeadNormalizationService> _logger;

    public LeadNormalizationService(
        ICustomerRepository repo,
        INameNormalizationService normalizer,
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        ILogger<LeadNormalizationService> logger)
    {
        _repo = repo;
        _normalizer = normalizer;
        _aiClients = aiClients;
        _settings = settings;
        _logger = logger;
    }

    public async Task<NormalizationStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var all = (await _repo.GetAllAsync(ct)).ToList();

        var total      = all.Count;
        var normalized = all.Count(c => c.NormalizedName != null);
        var pending    = total - normalized;
        var highConf   = all.Count(c => c.NormalizationScore >= 80);
        var lowConf    = all.Count(c => c.NormalizationScore is > 0 and < 80);
        var coverage   = total > 0 ? Math.Round(normalized * 100.0 / total, 1) : 0.0;

        return new NormalizationStatsDto
        {
            Total           = total,
            Normalized      = normalized,
            Pending         = pending,
            CoveragePercent = coverage,
            HighConfidence  = highConf,
            LowConfidence   = lowConf,
        };
    }

    public async Task<IReadOnlyList<NormalizationPreviewItemDto>> GetPreviewAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var all = (await _repo.GetAllAsync(ct))
            .OrderBy(c => c.NormalizedName == null ? 0 : 1)
            .ThenBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return all.Select(c =>
        {
            var suggestion = _normalizer.Normalize(c.Name, c.Email);
            return new NormalizationPreviewItemDto
            {
                CustomerId            = c.Id.ToString(),
                RawName               = c.Name,
                Email                 = c.Email,
                SuggestedName         = suggestion.NormalizedName.Length > 0 ? suggestion.NormalizedName : null,
                SuggestedScore        = suggestion.Score,
                SuggestedSource       = suggestion.Source.ToString(),
                CurrentNormalizedName = c.NormalizedName,
                CurrentScore          = c.NormalizationScore,
            };
        }).ToList();
    }

    public async Task<RunNormalizationResultDto> RunBatchAsync(
        bool forceReprocess = false, CancellationToken ct = default)
    {
        var all = (await _repo.GetAllAsync(ct)).ToList();

        var toProcess = forceReprocess
            ? all
            : all.Where(c => c.NormalizedName == null).ToList();

        int updated = 0, skipped = 0;

        foreach (var customer in toProcess)
        {
            var result = _normalizer.Normalize(customer.Name, customer.Email);

            if (result.Score == 0 || string.IsNullOrWhiteSpace(result.NormalizedName))
            {
                skipped++;
                continue;
            }

            customer.ApplyNormalization(
                result.NormalizedName,
                "system-deterministic",
                result.Score,
                result.Source);

            await _repo.UpdateAsync(customer, ct);
            updated++;
        }

        return new RunNormalizationResultDto
        {
            Processed = toProcess.Count,
            Updated   = updated,
            Skipped   = skipped,
        };
    }

    public async Task ApproveAsync(Guid customerId, string approvedName, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(customerId, ct)
            ?? throw new InvalidOperationException($"Customer {customerId} not found.");

        var cleanName = approvedName.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
            throw new ArgumentException("Approved name cannot be empty.");

        customer.ApplyNormalization(cleanName, "manual", 100, NormalizationSource.Manual);
        await _repo.UpdateAsync(customer, ct);
    }

    public async Task<NameNormalizationResult> NormalizeWithAiAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(customerId, ct)
            ?? throw new InvalidOperationException($"Customer {customerId} not found.");

        var (client, model) = PickAiClient();
        if (client is null)
        {
            _logger.LogWarning("[AI Normalize] No AI provider configured. Falling back to deterministic.");
            var fallback = _normalizer.Normalize(customer.Name, customer.Email);
            if (fallback.Score > 0)
                customer.ApplyNormalization(fallback.NormalizedName, "system-deterministic", fallback.Score, fallback.Source);
            await _repo.UpdateAsync(customer, ct);
            return fallback;
        }

        var providerConfig = _settings.GetProviderConfig(client.ProviderName);

        var systemPrompt = "You are a name extraction assistant. Extract ONLY the first name (given name) from the contact data provided. Return ONLY the first name in Title Case. If you cannot determine a first name, return an empty string. Never return anything else — no explanation, no punctuation, just the first name or empty string.";
        var userPrompt   = $"Contact: Name=\"{customer.Name}\", Email=\"{customer.Email ?? ""}\"";

        try
        {
            var options  = new AiClientOptions(providerConfig!.ApiKey!, "", model, Temperature: 0.1, MaxTokens: 20);
            var response = (await client.TransformAsync(systemPrompt, userPrompt, options, ct)).Trim();

            var validName = SanitizeAiName(response);
            if (string.IsNullOrEmpty(validName))
            {
                _logger.LogWarning("[AI Normalize] AI returned unusable response '{R}' for customer {Id}", response, customerId);
                return new NameNormalizationResult("", 0, NormalizationSource.AiFallback);
            }

            customer.ApplyNormalization(validName, $"ai:{client.ProviderName}", 80, NormalizationSource.AiFallback);
            await _repo.UpdateAsync(customer, ct);

            _logger.LogInformation("[AI Normalize] Customer {Id} normalized to '{N}' via {P}", customerId, validName, client.ProviderName);
            return new NameNormalizationResult(validName, 80, NormalizationSource.AiFallback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI Normalize] Error calling AI for customer {Id}", customerId);
            throw;
        }
    }

    private (IAiTextTransformClient? client, string model) PickAiClient()
    {
        foreach (var (providerKey, model) in AiProviderPreference)
        {
            var config = _settings.GetProviderConfig(providerKey);
            if (config is null || string.IsNullOrWhiteSpace(config.ApiKey))
                continue;

            var client = _aiClients.FirstOrDefault(c =>
                c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase));

            if (client is not null)
                return (client, model);
        }

        return (null, string.Empty);
    }

    private static string SanitizeAiName(string raw)
    {
        var clean = raw.Trim('"', '\'', '.', ',', ' ');

        if (string.IsNullOrWhiteSpace(clean) || clean.Length > 50)
            return string.Empty;

        if (clean.Any(c => c is '@' or '/' or '\\' or '<' or '>'))
            return string.Empty;

        if (clean.All(char.IsDigit))
            return string.Empty;

        return char.ToUpperInvariant(clean[0]) + clean[1..].ToLowerInvariant();
    }
}
