using Diax.Application.AI;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
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
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<HumanizeTextService> _logger;

    public HumanizeTextService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ILogger<HumanizeTextService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<HumanizeTextResponseDto> HumanizeAsync(HumanizeTextRequestDto request, Guid userId, CancellationToken ct = default)
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
        string? outputText = null;
        bool success = false;
        string? errorMsg = null;

        try
        {
            outputText = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
            success = true;

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("HumanizeText completed. RequestId: {RequestId}. OutputLength: {OutputLength}. Duration: {Duration}ms",
                requestId, outputText.Length, duration.TotalMilliseconds);

            // Track usage (fire and forget - don't block response)
            _ = Task.Run(async () =>
            {
                try
                {
                    var provider = await _providerRepository.GetByKeyAsync(providerKey, CancellationToken.None);
                    if (provider != null)
                    {
                        var modelKey = !string.IsNullOrWhiteSpace(request.Model) ? request.Model : null;
                        var model = modelKey != null
                            ? await _modelRepository.GetByProviderAndModelKeyAsync(provider.Id, modelKey, CancellationToken.None)
                            : (await _modelRepository.GetByProviderIdAsync(provider.Id, CancellationToken.None)).FirstOrDefault();

                        if (model != null)
                        {
                            await _usageTracking.LogUsageAsync(
                                userId: userId,
                                providerId: provider.Id,
                                modelId: model.Id,
                                featureType: "Humanization",
                                duration: duration,
                                success: success,
                                requestId: requestId,
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

            return new HumanizeTextResponseDto(
                OutputText: outputText,
                ProviderUsed: providerKey,
                ToneUsed: request.Tone,
                RequestId: requestId
            );
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            var duration = DateTime.UtcNow - startTime;

            // Track failed usage
            _ = Task.Run(async () =>
            {
                try
                {
                    var provider = await _providerRepository.GetByKeyAsync(providerKey, CancellationToken.None);
                    if (provider != null)
                    {
                        var models = await _modelRepository.GetByProviderIdAsync(provider.Id, CancellationToken.None);
                        var model = models.FirstOrDefault();
                        if (model != null)
                        {
                            await _usageTracking.LogUsageAsync(
                                userId: userId,
                                providerId: provider.Id,
                                modelId: model.Id,
                                featureType: "Humanization",
                                duration: duration,
                                success: false,
                                requestId: requestId,
                                errorMessage: errorMsg,
                                cancellationToken: CancellationToken.None
                            );
                        }
                    }
                }
                catch (Exception trackEx)
                {
                    _logger.LogError(trackEx, "Failed to track failed usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            throw;
        }
    }
}
