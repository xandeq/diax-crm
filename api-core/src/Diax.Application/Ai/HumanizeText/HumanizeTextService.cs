using Diax.Application.AI;
using Diax.Application.Ai.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Ai.HumanizeText;

/// <summary>
/// Serviço de humanização de texto usando provedores de IA.
/// Valida providers contra o banco de dados (única fonte de verdade).
/// </summary>
public class HumanizeTextService : IApplicationService, IHumanizeTextService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly ILogger<HumanizeTextService> _logger;
    private readonly IAiUsageLogRepository _usageLogRepository;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ITokenEstimator _tokenEstimator;
    private readonly ICurrentUserService _currentUserService;

    public HumanizeTextService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        ILogger<HumanizeTextService> logger,
        IAiUsageLogRepository usageLogRepository,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ITokenEstimator tokenEstimator,
        ICurrentUserService currentUserService)
    {
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _logger = logger;
        _usageLogRepository = usageLogRepository;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _tokenEstimator = tokenEstimator;
        _currentUserService = currentUserService;
    }

    public async Task<HumanizeTextResponseDto> HumanizeAsync(HumanizeTextRequestDto request, CancellationToken ct = default)
    {
        var providerKey = request.Provider.ToLower();

        // Validação do provider contra o banco de dados (única fonte de verdade)
        var isValidProvider = await _aiModelValidator.IsValidProviderAsync(providerKey, ct);
        if (!isValidProvider)
        {
            var activeProviders = await _aiModelValidator.GetActiveProviderKeysAsync(ct);
            var availableList = activeProviders.Any()
                ? string.Join(", ", activeProviders)
                : "Nenhum provider configurado no banco de dados.";

            throw new ArgumentException(
                $"Provedor '{request.Provider}' não está ativo ou não existe. Providers disponíveis: {availableList}");
        }

        // Obter configuração do provider (API key, base URL, etc.)
        var providerSettings = _settings.GetProviderConfig(providerKey)
            ?? throw new InvalidOperationException(
                $"Configuração não encontrada para o provider '{providerKey}'. " +
                "Verifique se o appsettings contém a seção PromptGenerator com as credenciais do provider.");

        if (string.IsNullOrWhiteSpace(providerSettings.ApiKey))
        {
            throw new InvalidOperationException(
                $"API Key não configurada para o provider '{providerKey}'. " +
                "Configure a chave em appsettings.json ou variáveis de ambiente.");
        }

        // Encontrar o client de IA correspondente
        var client = _aiClients.FirstOrDefault(c => c.ProviderName == providerKey)
            ?? throw new InvalidOperationException(
                $"Client de IA não encontrado para o provider '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        var options = new AiClientOptions(
            ApiKey: providerSettings.ApiKey,
            BaseUrl: providerSettings.BaseUrl ?? string.Empty,
            Model: !string.IsNullOrWhiteSpace(request.Model) ? request.Model : (providerSettings.Model ?? string.Empty),
            Temperature: request.Temperature ?? 0.7,
            MaxTokens: request.MaxTokens
        );

        var systemPrompt = HumanizeTextPromptBuilder.BuildSystemPrompt(request.Tone, request.Language ?? "pt-BR");
        var userPrompt = HumanizeTextPromptBuilder.BuildUserPrompt(request.InputText);

        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("HumanizeText started. RequestId: {RequestId}. Provider: {Provider}. Tone: {Tone}. InputLength: {InputLength}",
            requestId, providerKey, request.Tone, request.InputText.Length);

        var startTime = DateTime.UtcNow;
        var outputText = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
        var duration = DateTime.UtcNow - startTime;

        // Fire-and-forget AI usage logging (non-blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                // 1. Estimate tokens
                var tokensInput = _tokenEstimator.EstimateTokens(systemPrompt + userPrompt);
                var tokensOutput = _tokenEstimator.EstimateTokens(outputText);

                // 2. Get provider and model from DB
                var provider = await _providerRepository.GetByKeyAsync(providerKey, ct);
                if (provider == null)
                {
                    _logger.LogWarning("AI usage logging skipped: Provider '{Provider}' not found", providerKey);
                    return;
                }

                var models = await _modelRepository.GetByProviderIdAsync(provider.Id, ct);
                var model = models.FirstOrDefault(m =>
                    m.ModelKey.Equals(options.Model, StringComparison.OrdinalIgnoreCase));

                if (model == null)
                {
                    _logger.LogWarning("AI usage logging skipped: Model '{Model}' not found", options.Model);
                    return;
                }

                // 3. Calculate cost (InputCostHint/OutputCostHint = cost per 1000 tokens)
                var inputCost = (tokensInput / 1000m) * (model.InputCostHint ?? 0m);
                var outputCost = (tokensOutput / 1000m) * (model.OutputCostHint ?? 0m);
                var estimatedCost = inputCost + outputCost;

                // 4. Get current user (nullable)
                var userId = _currentUserService.UserId;

                // 5. Create and save log
                var usageLog = new AiUsageLog(
                    userId: userId,
                    providerId: provider.Id,
                    modelId: model.Id,
                    tokensInput: tokensInput,
                    tokensOutput: tokensOutput,
                    costEstimated: estimatedCost,
                    requestType: "humanize_text"
                );

                await _usageLogRepository.AddAsync(usageLog, ct);
                await _usageLogRepository.SaveChangesAsync(ct);

                _logger.LogDebug(
                    "AI usage logged: RequestId={RequestId}, Tokens={Tokens}, Cost=${Cost:F6}",
                    requestId, tokensInput + tokensOutput, estimatedCost);
            }
            catch (Exception ex)
            {
                // CRITICAL: Swallow exception to prevent breaking main flow
                _logger.LogError(ex,
                    "Failed to log AI usage for RequestId={RequestId}. Non-critical error ignored.",
                    requestId);
            }
        }, ct);

        _logger.LogInformation("HumanizeText completed. RequestId: {RequestId}. OutputLength: {OutputLength}. Duration: {Duration}ms",
            requestId, outputText.Length, duration.TotalMilliseconds);

        return new HumanizeTextResponseDto(
            OutputText: outputText,
            ProviderUsed: providerKey,
            ToneUsed: request.Tone,
            RequestId: requestId
        );
    }
}
