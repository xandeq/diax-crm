using Diax.Application.AI.QuotaManagement;
using Diax.Application.AI.Services;
using Diax.Application.AI.VideoGeneration.Dtos;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Shared;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Diax.Application.AI.VideoGeneration;

public class VideoGenerationService : IApplicationService, IVideoGenerationService
{
    private readonly IEnumerable<IAiVideoGenerationClient> _videoClients;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiQuotaService _quotaService;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiProviderCredentialRepository _credentialRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly PromptGeneratorSettings _promptSettings;
    private readonly ILogger<VideoGenerationService> _logger;

    public VideoGenerationService(
        IEnumerable<IAiVideoGenerationClient> videoClients,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiQuotaService quotaService,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IAiProviderCredentialRepository credentialRepository,
        IApiKeyEncryptionService encryptionService,
        PromptGeneratorSettings promptSettings,
        ILogger<VideoGenerationService> logger)
    {
        _videoClients = videoClients;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _quotaService = quotaService;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _promptSettings = promptSettings;
        _logger = logger;
    }

    public async Task<VideoGenerationResponseDto> GenerateAsync(
        VideoGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        var providerKey = request.Provider.ToLower();

        // 1. Validate provider
        var isValidProvider = await _aiModelValidator.IsValidProviderAsync(providerKey, ct);
        if (!isValidProvider)
        {
            var activeProviders = await _aiModelValidator.GetActiveProviderKeysAsync(ct);
            var availableList = activeProviders.Any()
                ? string.Join(", ", activeProviders)
                : "Nenhum provider configurado.";
            throw new ArgumentException(
                $"Provedor '{request.Provider}' não está ativo ou não existe. Providers disponíveis: {availableList}");
        }

        // 2. Resolve provider and model from DB
        var provider = await _providerRepository.GetByKeyAsync(providerKey, ct)
            ?? throw new ArgumentException($"Provider '{providerKey}' não encontrado.");

        var model = await _modelRepository.GetByProviderAndModelKeyAsync(provider.Id, request.Model, ct)
            ?? throw new ArgumentException(
                $"Modelo '{request.Model}' não encontrado para o provider '{providerKey}'.");

        if (!model.IsEnabled)
            throw new ArgumentException($"Modelo '{request.Model}' está desabilitado.");

        if (!model.SupportsVideoGeneration())
            throw new ArgumentException(
                $"Modelo '{request.Model}' não suporta geração de vídeo. " +
                "Verifique se o CapabilitiesJson do modelo está configurado corretamente.");

        // 2.5. Check quota before proceeding
        var quotaCheckResult = await _quotaService.CanUserGenerateAsync(provider.Id, 1, ct);
        if (!quotaCheckResult.IsSuccess)
        {
            _logger.LogWarning(
                "VideoGeneration quota check failed for provider '{Provider}': {Error}",
                providerKey, quotaCheckResult.Error?.Message);
            throw new InvalidOperationException(quotaCheckResult.Error?.Message ?? "Quota limit exceeded");
        }

        // 3. Get API key — DB first, then appsettings fallback
        string apiKey;
        var credential = await _credentialRepository.GetByProviderIdAsync(provider.Id, ct);
        if (credential != null && credential.IsConfigured())
        {
            _logger.LogDebug("VideoGeneration: using DB credential for provider '{Provider}'", providerKey);
            try
            {
                apiKey = _encryptionService.Decrypt(credential.ApiKeyEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "VideoGeneration: credential decryption failed for '{Provider}' (keys may have been rotated), trying appsettings fallback",
                    providerKey);
                var providerConfig = _promptSettings.GetProviderConfig(providerKey);
                if (providerConfig == null || string.IsNullOrWhiteSpace(providerConfig.ApiKey))
                    throw new InvalidOperationException(
                        $"API Key inválida para '{providerKey}': falha na descriptografia e nenhum fallback configurado.");
                apiKey = providerConfig.ApiKey;
            }
        }
        else
        {
            var providerConfig = _promptSettings.GetProviderConfig(providerKey);
            if (providerConfig == null || string.IsNullOrWhiteSpace(providerConfig.ApiKey))
                throw new InvalidOperationException(
                    $"API Key não configurada para o provider '{providerKey}'. " +
                    "Configure a chave em Administração > AI > Providers ou nas variáveis de ambiente.");

            _logger.LogDebug("VideoGeneration: using appsettings fallback for '{Provider}'", providerKey);
            apiKey = providerConfig.ApiKey;
        }

        // 4. Find the matching video client
        var client = _videoClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Client de geração de vídeo não encontrado para '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        // 5. Build options and generate
        var options = new VideoGenerationOptions(
            ApiKey: apiKey,
            BaseUrl: provider.BaseUrl ?? string.Empty,
            Model: request.Model,
            DurationSeconds: request.DurationSeconds,
            Width: request.Width,
            Height: request.Height,
            AspectRatio: request.AspectRatio,
            NegativePrompt: request.NegativePrompt,
            Seed: request.Seed
        );

        var requestId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "VideoGeneration started. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}",
            requestId, providerKey, request.Model);

        try
        {
            var result = await client.GenerateAsync(request.Prompt, options, request.ReferenceImageBase64, ct);

            var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "VideoGeneration completed. RequestId: {RequestId}. Duration: {Duration}ms",
                requestId, durationMs);

            // Get updated quota status BEFORE fire-and-forget tasks to avoid DbContext concurrency issues
            var quotaStatus = await _quotaService.GetQuotaStatusAsync(provider.Id);

            // Record quota usage (fire and forget — after sync DB ops are done)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _quotaService.RecordGenerationAsync(provider.Id, 1, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to record quota usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            // Record success + track usage (fire and forget)
            model.RecordSuccess();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _modelRepository.UpdateFailureTrackingAsync(model, CancellationToken.None);
                    await _usageTracking.LogUsageAsync(
                        userId: userId,
                        providerId: provider.Id,
                        modelId: model.Id,
                        featureType: "VideoGeneration",
                        duration: TimeSpan.FromMilliseconds(durationMs),
                        success: true,
                        requestId: requestId,
                        inputTokens: (request.Prompt?.Length ?? 0),
                        outputTokens: 1,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track video usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            return new VideoGenerationResponseDto(
                ProviderUsed: providerKey,
                ModelUsed: request.Model,
                RequestId: requestId,
                DurationMs: durationMs,
                VideoUrl: result.VideoUrl,
                ThumbnailUrl: result.ThumbnailUrl,
                QuotaStatus: quotaStatus
            );
        }
        catch (Exception ex)
        {
            var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Categorize the error for structured observability
            var httpStatus    = AiErrorCategorizationHelper.ExtractHttpStatusCode(ex);
            var errorCategory = AiErrorCategorizationHelper.Categorize(ex, httpStatus);
            var sanitizedMsg  = AiErrorCategorizationHelper.SanitizeForLog(ex.Message);

            _logger.LogError(ex,
                "VideoGeneration failed. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. " +
                "ErrorCategory: {ErrorCategory}. HttpStatus: {HttpStatus}. Message: {Message}",
                requestId, providerKey, request.Model, errorCategory, httpStatus, sanitizedMsg);

            // Record failure on model + track usage (fire and forget)
            model.RecordFailure(errorCategory, sanitizedMsg);
            _ = Task.Run(async () =>
            {
                try
                {
                    await _modelRepository.UpdateFailureTrackingAsync(model, CancellationToken.None);
                    await _usageTracking.LogUsageAsync(
                        userId: userId,
                        providerId: provider.Id,
                        modelId: model.Id,
                        featureType: "VideoGeneration",
                        duration: TimeSpan.FromMilliseconds(durationMs),
                        success: false,
                        requestId: requestId,
                        errorMessage: sanitizedMsg,
                        errorCategory: errorCategory,
                        httpStatusCode: httpStatus,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception trackEx)
                {
                    _logger.LogError(trackEx, "Failed to track failed video usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            throw new AiProviderException(ex.Message, errorCategory, ex);
        }
    }
}
