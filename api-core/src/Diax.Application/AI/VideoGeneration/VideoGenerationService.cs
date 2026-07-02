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
    private readonly IAiCatalogService _catalogService;
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
        IAiCatalogService catalogService,
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
        _catalogService = catalogService;
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
        ValidateRequest(request);

        var requestedProviderKey = request.Provider.ToLower();
        var requestedCandidate = await ResolveRequestedCandidateAsync(requestedProviderKey, request.Model, ct);

        var requestId = Guid.NewGuid().ToString();
        var attempted = new List<string>();
        var attemptErrors = new List<string>();
        var lastErrorCategory = AiErrorCategory.Unknown;

        GenerationCandidate? candidate = requestedCandidate;

        while (candidate != null)
        {
            attempted.Add(candidate.ProviderKey);
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "VideoGeneration attempt {Attempt}. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. Fallback: {IsFallback}",
                attempted.Count, requestId, candidate.ProviderKey, candidate.ModelKey, attempted.Count > 1);

            try
            {
                // Quota local do provider antes de gastar a chamada
                var quotaCheck = await _quotaService.CanUserGenerateAsync(candidate.Provider.Id, 1, ct);
                if (!quotaCheck.IsSuccess)
                    throw new AiProviderException(
                        quotaCheck.Error?.Message ?? $"Quota local esgotada para '{candidate.ProviderKey}'.",
                        AiErrorCategory.QuotaExhausted);

                var options = new VideoGenerationOptions(
                    ApiKey: candidate.ApiKey,
                    BaseUrl: candidate.Provider.BaseUrl ?? string.Empty,
                    Model: candidate.ModelKey,
                    DurationSeconds: request.DurationSeconds,
                    Width: request.Width,
                    Height: request.Height,
                    AspectRatio: request.AspectRatio,
                    NegativePrompt: request.NegativePrompt,
                    Seed: request.Seed);

                var result = await candidate.Client.GenerateAsync(
                    request.Prompt, options, request.ReferenceImageBase64, ct);

                if (result == null || string.IsNullOrWhiteSpace(result.VideoUrl))
                    throw new AiProviderException(
                        $"Provider '{candidate.ProviderKey}' retornou resposta vazia (sem URL de vídeo).",
                        AiErrorCategory.ProviderUnavailable);

                var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation(
                    "VideoGeneration completed. RequestId: {RequestId}. Provider: {Provider}. Duration: {Duration}ms. Fallback: {Fallback}",
                    requestId, candidate.ProviderKey, durationMs, attempted.Count > 1);

                // Tracking awaited e best-effort (sem Task.Run — DbContext é scoped).
                candidate.Model.RecordSuccess();
                QuotaStatusDto? quotaStatus = null;
                try
                {
                    await _quotaService.RecordGenerationAsync(candidate.Provider.Id, 1, CancellationToken.None);
                    quotaStatus = await _quotaService.GetQuotaStatusAsync(candidate.Provider.Id, CancellationToken.None);
                    await _modelRepository.UpdateFailureTrackingAsync(candidate.Model, CancellationToken.None);
                    await _usageTracking.LogUsageAsync(
                        userId: userId,
                        providerId: candidate.Provider.Id,
                        modelId: candidate.Model.Id,
                        featureType: "VideoGeneration",
                        duration: TimeSpan.FromMilliseconds(durationMs),
                        success: true,
                        requestId: requestId,
                        inputTokens: request.Prompt?.Length ?? 0,
                        outputTokens: 1,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track video usage for request {RequestId}", requestId);
                }

                return new VideoGenerationResponseDto(
                    ProviderUsed: candidate.ProviderKey,
                    ModelUsed: candidate.ModelKey,
                    RequestId: requestId,
                    DurationMs: durationMs,
                    VideoUrl: result.VideoUrl,
                    ThumbnailUrl: result.ThumbnailUrl,
                    QuotaStatus: quotaStatus,
                    FallbackOccurred: attempted.Count > 1,
                    RequestedProvider: requestedProviderKey,
                    AttemptedProviders: attempted,
                    EstimatedCostUsd: AiGenerationCostEstimator.EstimateVideoCostUsd(
                        candidate.ProviderKey, candidate.ModelKey, request.DurationSeconds));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var httpStatus = AiErrorCategorizationHelper.ExtractHttpStatusCode(ex);
                var errorCategory = ex is AiProviderException aiEx
                    ? aiEx.ErrorCode
                    : AiErrorCategorizationHelper.Categorize(ex, httpStatus);
                var sanitizedMsg = AiErrorCategorizationHelper.SanitizeForLog(ex.Message);

                lastErrorCategory = errorCategory;
                attemptErrors.Add($"{candidate.ProviderKey}: [{errorCategory}] {sanitizedMsg}");

                _logger.LogError(ex,
                    "VideoGeneration attempt failed. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. " +
                    "ErrorCategory: {ErrorCategory}. HttpStatus: {HttpStatus}. Message: {Message}",
                    requestId, candidate.ProviderKey, candidate.ModelKey, errorCategory, httpStatus, sanitizedMsg);

                await TrackAttemptFailureAsync(
                    userId, candidate, errorCategory, sanitizedMsg, httpStatus, requestId, durationMs);

                GenerationCandidate? next = null;
                if (request.AllowFallback && AiMediaFallbackPolicy.ShouldFallback(errorCategory))
                {
                    next = await ResolveNextFallbackCandidateAsync(
                        userId, attempted, request.ReferenceImageBase64 != null, ct);

                    if (next != null)
                        _logger.LogWarning(
                            "VideoGeneration FALLBACK. RequestId: {RequestId}. {From} → {To} (motivo: {Category})",
                            requestId, candidate.ProviderKey, next.ProviderKey, errorCategory);
                }

                candidate = next;
            }
        }

        var aggregate = string.Join(" | ", attemptErrors);
        throw new AiProviderException(
            attempted.Count > 1
                ? $"Todos os {attempted.Count} providers falharam. Detalhes: {aggregate}"
                : aggregate,
            lastErrorCategory);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Resolução de candidatos
    // ─────────────────────────────────────────────────────────────────────────
    private sealed record GenerationCandidate(
        string ProviderKey,
        AiProvider Provider,
        AiModel Model,
        string ModelKey,
        string ApiKey,
        IAiVideoGenerationClient Client);

    private async Task<GenerationCandidate> ResolveRequestedCandidateAsync(
        string providerKey, string modelKey, CancellationToken ct)
    {
        var isValidProvider = await _aiModelValidator.IsValidProviderAsync(providerKey, ct);
        if (!isValidProvider)
        {
            var activeProviders = await _aiModelValidator.GetActiveProviderKeysAsync(ct);
            var availableList = activeProviders.Any()
                ? string.Join(", ", activeProviders)
                : "Nenhum provider configurado.";
            throw new ArgumentException(
                $"Provedor '{providerKey}' não está ativo ou não existe. Providers disponíveis: {availableList}");
        }

        var provider = await _providerRepository.GetByKeyAsync(providerKey, ct)
            ?? throw new ArgumentException($"Provider '{providerKey}' não encontrado.");

        var model = await _modelRepository.GetByProviderAndModelKeyAsync(provider.Id, modelKey, ct)
            ?? throw new ArgumentException(
                $"Modelo '{modelKey}' não encontrado para o provider '{providerKey}'.");

        if (!model.IsEnabled)
            throw new ArgumentException($"Modelo '{modelKey}' está desabilitado.");

        if (!model.SupportsVideoGeneration())
            throw new ArgumentException(
                $"Modelo '{modelKey}' não suporta geração de vídeo. " +
                "Verifique se o CapabilitiesJson do modelo está configurado corretamente.");

        var client = FindClient(providerKey)
            ?? throw new InvalidOperationException(
                $"Client de geração de vídeo não encontrado para '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        var apiKey = await TryResolveApiKeyAsync(provider, providerKey)
            ?? throw new AiProviderException(
                $"API Key não configurada para o provider '{providerKey}'. " +
                "Configure a chave em Administração > AI > Providers ou nas variáveis de ambiente.",
                AiErrorCategory.ConfigurationMissing);

        return new GenerationCandidate(providerKey, provider, model, model.ModelKey, apiKey, client);
    }

    private async Task<GenerationCandidate?> ResolveNextFallbackCandidateAsync(
        Guid userId,
        List<string> alreadyAttempted,
        bool requiresImageToVideo,
        CancellationToken ct)
    {
        foreach (var providerKey in AiMediaFallbackPolicy.VideoProviderOrder)
        {
            if (alreadyAttempted.Contains(providerKey, StringComparer.OrdinalIgnoreCase))
                continue;

            try
            {
                var client = FindClient(providerKey);
                if (client == null) continue;
                if (requiresImageToVideo && !client.SupportsImageToVideo) continue;

                if (!await _aiModelValidator.IsValidProviderAsync(providerKey, ct)) continue;

                var provider = await _providerRepository.GetByKeyAsync(providerKey, ct);
                if (provider == null) continue;

                var apiKey = await TryResolveApiKeyAsync(provider, providerKey);
                if (apiKey == null) continue;

                var models = (await _modelRepository.GetEnabledByProviderAsync(provider.Id, ct))
                    .Where(m => m.SupportsVideoGeneration())
                    .ToList();
                if (models.Count == 0) continue;

                var model = AiMediaFallbackPolicy.PreferredVideoModel.TryGetValue(providerKey, out var preferred)
                    ? models.FirstOrDefault(m => m.ModelKey.Equals(preferred, StringComparison.OrdinalIgnoreCase)) ?? models[0]
                    : models[0];

                // RBAC: fallback respeita as permissões de grupo do usuário.
                var hasAccess = await _catalogService.ValidateUserAccessAsync(userId, providerKey, model.ModelKey, ct);
                if (!hasAccess)
                {
                    _logger.LogDebug(
                        "VideoGeneration fallback: usuário {UserId} sem acesso a {Provider}/{Model} — pulando",
                        userId, providerKey, model.ModelKey);
                    continue;
                }

                return new GenerationCandidate(providerKey, provider, model, model.ModelKey, apiKey, client);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "VideoGeneration fallback: erro avaliando candidato '{Provider}' — pulando", providerKey);
            }
        }

        return null;
    }

    private IAiVideoGenerationClient? FindClient(string providerKey) =>
        _videoClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase));

    private async Task<string?> TryResolveApiKeyAsync(AiProvider provider, string providerKey)
    {
        var credential = await _credentialRepository.GetByProviderIdAsync(provider.Id);
        if (credential != null && credential.IsConfigured())
        {
            try
            {
                return _encryptionService.Decrypt(credential.ApiKeyEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "VideoGeneration: falha ao descriptografar credencial de '{Provider}' (chaves rotacionadas?), tentando appsettings",
                    providerKey);
            }
        }

        var providerConfig = _promptSettings.GetProviderConfig(providerKey);
        return string.IsNullOrWhiteSpace(providerConfig?.ApiKey) ? null : providerConfig.ApiKey;
    }

    private static void ValidateRequest(VideoGenerationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt) && string.IsNullOrWhiteSpace(request.ReferenceImageBase64))
            throw new ArgumentException("Informe um prompt de texto ou uma imagem de referência para gerar o vídeo.");

        if (request.DurationSeconds is < 1 or > 30)
            throw new ArgumentException("DurationSeconds deve estar entre 1 e 30 segundos.");

        if (request.Width is < 256 or > 4096 || request.Height is < 256 or > 4096)
            throw new ArgumentException("Dimensões devem estar entre 256 e 4096 pixels.");
    }

    private async Task TrackAttemptFailureAsync(
        Guid userId,
        GenerationCandidate candidate,
        string errorCategory,
        string sanitizedMsg,
        int? httpStatus,
        string requestId,
        int durationMs)
    {
        candidate.Model.RecordFailure(errorCategory, sanitizedMsg);
        try
        {
            await _modelRepository.UpdateFailureTrackingAsync(candidate.Model, CancellationToken.None);
            await _usageTracking.LogUsageAsync(
                userId: userId,
                providerId: candidate.Provider.Id,
                modelId: candidate.Model.Id,
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
    }
}
