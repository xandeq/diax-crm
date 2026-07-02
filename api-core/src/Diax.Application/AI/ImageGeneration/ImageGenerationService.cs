using Diax.Application.AI.ImageGeneration.Dtos;
using Diax.Application.AI.MediaStorage;
using Diax.Application.AI.QuotaManagement;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Diax.Domain.ImageGeneration;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Diax.Application.AI.ImageGeneration;

public class ImageGenerationService : IApplicationService, IImageGenerationService
{
    private const int MaxUrlLength = 1000; // limite das colunas storage_url/provider_url

    private readonly IEnumerable<IAiImageGenerationClient> _imageClients;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiQuotaService _quotaService;
    private readonly IAiCatalogService _catalogService;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiProviderCredentialRepository _credentialRepository;
    private readonly IImageGenerationProjectRepository _projectRepository;
    private readonly IGeneratedImageRepository _generatedImageRepository;
    private readonly IImageTemplateRepository _templateRepository;
    private readonly IGeneratedMediaStorageService _mediaStorage;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PromptGeneratorSettings _promptSettings;
    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        IEnumerable<IAiImageGenerationClient> imageClients,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiQuotaService quotaService,
        IAiCatalogService catalogService,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IAiProviderCredentialRepository credentialRepository,
        IImageGenerationProjectRepository projectRepository,
        IGeneratedImageRepository generatedImageRepository,
        IImageTemplateRepository templateRepository,
        IGeneratedMediaStorageService mediaStorage,
        IApiKeyEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        PromptGeneratorSettings promptSettings,
        ILogger<ImageGenerationService> logger)
    {
        _imageClients = imageClients;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _quotaService = quotaService;
        _catalogService = catalogService;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _credentialRepository = credentialRepository;
        _projectRepository = projectRepository;
        _generatedImageRepository = generatedImageRepository;
        _templateRepository = templateRepository;
        _mediaStorage = mediaStorage;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _promptSettings = promptSettings;
        _logger = logger;
    }

    public async Task<ImageGenerationResponseDto> GenerateAsync(
        ImageGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        ValidateRequest(request);

        var requestedProviderKey = request.Provider.ToLower();

        // 1. Resolve o candidato solicitado — erros aqui são do usuário (sem fallback)
        var requestedCandidate = await ResolveRequestedCandidateAsync(requestedProviderKey, request.Model, ct);

        // 2. Projeto (criado uma vez, sobrevive às tentativas de fallback)
        var project = await ResolveProjectAsync(request, userId, ct);

        // 3. Prompt final (template do projeto, se houver)
        string? templatePrompt = null;
        if (project.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(project.TemplateId.Value, ct);
            templatePrompt = template?.PromptTemplate;
        }

        var finalPrompt = ImagePromptBuilder.Build(request.Prompt, templatePrompt, project.ParametersJson);

        var requestId = Guid.NewGuid().ToString();
        var attempted = new List<string>();
        var attemptErrors = new List<string>();

        project.SetProcessing();
        await _projectRepository.UpdateAsync(project, ct);

        // 4. Loop de tentativas: provider solicitado primeiro, depois cadeia de fallback
        GenerationCandidate? candidate = requestedCandidate;
        string lastErrorCategory = AiErrorCategory.Unknown;

        while (candidate != null)
        {
            attempted.Add(candidate.ProviderKey);
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "ImageGeneration attempt {Attempt}. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. Size: {W}x{H}. Fallback: {IsFallback}",
                attempted.Count, requestId, candidate.ProviderKey, candidate.ModelKey,
                request.Width, request.Height, attempted.Count > 1);

            try
            {
                // 4a. Quota local do provider (evita gastar chamada quando já esgotou)
                var quotaCheck = await _quotaService.CanUserGenerateAsync(candidate.Provider.Id, request.NumberOfImages, ct);
                if (!quotaCheck.IsSuccess)
                    throw new AiProviderException(
                        quotaCheck.Error?.Message ?? $"Quota local esgotada para '{candidate.ProviderKey}'.",
                        AiErrorCategory.QuotaExhausted);

                var options = new ImageGenerationOptions(
                    ApiKey: candidate.ApiKey,
                    BaseUrl: candidate.Provider.BaseUrl ?? string.Empty,
                    Model: candidate.ModelKey,
                    Width: request.Width,
                    Height: request.Height,
                    NumberOfImages: request.NumberOfImages,
                    NegativePrompt: request.NegativePrompt,
                    Seed: request.Seed,
                    Style: request.Style,
                    Quality: request.Quality);

                var results = await candidate.Client.GenerateAsync(
                    finalPrompt, options, request.ReferenceImageBase64, ct);

                if (results == null || results.Count == 0 || results.All(r => string.IsNullOrWhiteSpace(r.ImageUrl)))
                    throw new AiProviderException(
                        $"Provider '{candidate.ProviderKey}' retornou resposta vazia (nenhuma imagem).",
                        AiErrorCategory.ProviderUnavailable);

                var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                return await PersistSuccessAsync(
                    request, userId, project, candidate, results, finalPrompt,
                    requestId, durationMs, requestedProviderKey, attempted, ct);
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
                    "ImageGeneration attempt failed. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. " +
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
                            "ImageGeneration FALLBACK. RequestId: {RequestId}. {From} → {To} (motivo: {Category})",
                            requestId, candidate.ProviderKey, next.ProviderKey, errorCategory);
                }

                candidate = next;
            }
        }

        // 5. Todas as tentativas falharam
        project.SetFailed();
        await _projectRepository.UpdateAsync(project, ct);
        await TrySaveChangesAsync(requestId, CancellationToken.None);

        var aggregate = string.Join(" | ", attemptErrors);
        throw new AiProviderException(
            attempted.Count > 1
                ? $"Todos os {attempted.Count} providers falharam. Detalhes: {aggregate}"
                : aggregate,
            lastErrorCategory);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sucesso: storage durável + persistência explícita + tracking
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<ImageGenerationResponseDto> PersistSuccessAsync(
        ImageGenerationRequestDto request,
        Guid userId,
        ImageGenerationProject project,
        GenerationCandidate candidate,
        List<ImageGenerationResult> results,
        string finalPrompt,
        string requestId,
        int durationMs,
        string requestedProviderKey,
        List<string> attempted,
        CancellationToken ct)
    {
        var validResults = results.Where(r => !string.IsNullOrWhiteSpace(r.ImageUrl)).ToList();
        var pairs = new List<(GeneratedImage Entity, ImageGenerationResult Result)>();

        foreach (var result in validResults)
        {
            var providerUrl = !result.IsBase64 && result.ImageUrl.Length <= MaxUrlLength
                ? result.ImageUrl
                : null;

            var image = new GeneratedImage(
                projectId: project.Id,
                userId: userId,
                providerId: candidate.Provider.Id,
                modelId: candidate.Model.Id,
                prompt: finalPrompt,
                width: request.Width,
                height: request.Height,
                durationMs: durationMs,
                success: true,
                revisedPrompt: result.RevisedPrompt,
                providerUrl: providerUrl,
                storageUrl: null, // preenchido abaixo se o storage local funcionar
                seed: result.Seed);

            // Storage próprio: URLs de provider expiram e base64 não cabe no banco.
            var storedUrl = await _mediaStorage.TrySaveImageAsync(result.ImageUrl, result.IsBase64, image.Id, ct);
            if (!string.IsNullOrWhiteSpace(storedUrl) && storedUrl.Length <= MaxUrlLength)
                image.SetStorageUrl(storedUrl);
            else if (result.IsBase64)
                _logger.LogWarning(
                    "ImageGeneration: storage local falhou para imagem base64 {ImageId} — " +
                    "a imagem será entregue na resposta mas não ficará no histórico. RequestId: {RequestId}",
                    image.Id, requestId);

            await _generatedImageRepository.AddAsync(image, ct);
            pairs.Add((image, result));
        }

        project.SetCompleted();
        await _projectRepository.UpdateAsync(project, ct);

        // Persistência explícita — antes disso NADA foi commitado (AddAsync só faz stage).
        var persisted = await TrySaveChangesAsync(requestId, ct);

        _logger.LogInformation(
            "ImageGeneration completed. RequestId: {RequestId}. Provider: {Provider}. Images: {Count}. " +
            "Duration: {Duration}ms. Persisted: {Persisted}. Fallback: {Fallback}",
            requestId, candidate.ProviderKey, pairs.Count, durationMs, persisted, attempted.Count > 1);

        // Tracking awaited (sem Task.Run — DbContext é scoped e morre com o request).
        // Best-effort: nunca falha a resposta por causa de tracking.
        candidate.Model.RecordSuccess();
        try
        {
            await _modelRepository.UpdateFailureTrackingAsync(candidate.Model, CancellationToken.None);
            await _quotaService.RecordGenerationAsync(candidate.Provider.Id, pairs.Count, CancellationToken.None);
            await _usageTracking.LogUsageAsync(
                userId: userId,
                providerId: candidate.Provider.Id,
                modelId: candidate.Model.Id,
                featureType: "ImageGeneration",
                duration: TimeSpan.FromMilliseconds(durationMs),
                success: true,
                requestId: requestId,
                inputTokens: finalPrompt.Length,
                outputTokens: pairs.Count,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track usage for request {RequestId}", requestId);
        }

        return new ImageGenerationResponseDto(
            ProjectId: project.Id,
            ProviderUsed: candidate.ProviderKey,
            ModelUsed: candidate.ModelKey,
            RequestId: requestId,
            DurationMs: durationMs,
            Images: pairs.Select(p => new GeneratedImageDto(
                Id: p.Entity.Id,
                // Preferir URL durável; senão devolver o resultado cru (o frontend
                // renderiza base64/data URI e URLs do provider normalmente).
                ImageUrl: p.Entity.StorageUrl ?? p.Result.ImageUrl,
                RevisedPrompt: p.Entity.RevisedPrompt,
                Seed: p.Entity.Seed,
                Width: p.Entity.Width,
                Height: p.Entity.Height
            )).ToList(),
            FallbackOccurred: attempted.Count > 1,
            RequestedProvider: requestedProviderKey,
            AttemptedProviders: attempted
        );
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
        IAiImageGenerationClient Client);

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

        if (!model.SupportsImageGeneration())
            throw new ArgumentException(
                $"Modelo '{modelKey}' não suporta geração de imagens. " +
                "Verifique se o CapabilitiesJson do modelo está configurado corretamente.");

        var client = FindClient(providerKey)
            ?? throw new InvalidOperationException(
                $"Client de geração de imagem não encontrado para '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        var apiKey = await TryResolveApiKeyAsync(provider, providerKey)
            ?? throw new AiProviderException(
                $"API Key não configurada para o provider '{providerKey}'. " +
                "Configure a chave em Administração > AI > Providers ou nas variáveis de ambiente.",
                AiErrorCategory.ConfigurationMissing);

        return new GenerationCandidate(providerKey, provider, model, model.ModelKey, apiKey, client);
    }

    /// <summary>
    /// Próximo provider elegível da cadeia de fallback:
    /// ativo, com client registrado, key configurada, modelo habilitado com capability
    /// de imagem e (se houver imagem de referência) suporte a image-to-image.
    /// </summary>
    private async Task<GenerationCandidate?> ResolveNextFallbackCandidateAsync(
        Guid userId,
        List<string> alreadyAttempted,
        bool requiresImageToImage,
        CancellationToken ct)
    {
        foreach (var providerKey in AiMediaFallbackPolicy.ImageProviderOrder)
        {
            if (alreadyAttempted.Contains(providerKey, StringComparer.OrdinalIgnoreCase))
                continue;

            try
            {
                var client = FindClient(providerKey);
                if (client == null) continue;
                if (requiresImageToImage && !client.SupportsImageToImage) continue;

                if (!await _aiModelValidator.IsValidProviderAsync(providerKey, ct)) continue;

                var provider = await _providerRepository.GetByKeyAsync(providerKey, ct);
                if (provider == null) continue;

                var apiKey = await TryResolveApiKeyAsync(provider, providerKey);
                if (apiKey == null) continue;

                var models = (await _modelRepository.GetEnabledByProviderAsync(provider.Id, ct))
                    .Where(m => m.SupportsImageGeneration())
                    .ToList();
                if (models.Count == 0) continue;

                var model = AiMediaFallbackPolicy.PreferredImageModel.TryGetValue(providerKey, out var preferred)
                    ? models.FirstOrDefault(m => m.ModelKey.Equals(preferred, StringComparison.OrdinalIgnoreCase)) ?? models[0]
                    : models[0];

                // RBAC: fallback não pode dar ao usuário acesso a provider/modelo que ele não tem.
                var hasAccess = await _catalogService.ValidateUserAccessAsync(userId, providerKey, model.ModelKey, ct);
                if (!hasAccess)
                {
                    _logger.LogDebug(
                        "ImageGeneration fallback: usuário {UserId} sem acesso a {Provider}/{Model} — pulando",
                        userId, providerKey, model.ModelKey);
                    continue;
                }

                return new GenerationCandidate(providerKey, provider, model, model.ModelKey, apiKey, client);
            }
            catch (Exception ex)
            {
                // Falha ao avaliar um candidato nunca interrompe a cadeia — só pula.
                _logger.LogWarning(ex,
                    "ImageGeneration fallback: erro avaliando candidato '{Provider}' — pulando", providerKey);
            }
        }

        return null;
    }

    private IAiImageGenerationClient? FindClient(string providerKey) =>
        _imageClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Chave da API: credencial do banco (criptografada) primeiro, appsettings/env como fallback.
    /// Retorna null quando não há chave utilizável.
    /// </summary>
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
                    "ImageGeneration: falha ao descriptografar credencial de '{Provider}' (chaves rotacionadas?), tentando appsettings",
                    providerKey);
            }
        }

        var providerConfig = _promptSettings.GetProviderConfig(providerKey);
        return string.IsNullOrWhiteSpace(providerConfig?.ApiKey) ? null : providerConfig.ApiKey;
    }

    private async Task<ImageGenerationProject> ResolveProjectAsync(
        ImageGenerationRequestDto request, Guid userId, CancellationToken ct)
    {
        if (request.ProjectId.HasValue)
        {
            var existing = await _projectRepository.GetByIdAsync(request.ProjectId.Value, ct)
                ?? throw new ArgumentException($"Projeto '{request.ProjectId}' não encontrado.");
            return existing;
        }

        var project = new ImageGenerationProject(
            userId: userId,
            name: $"Geração {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        await _projectRepository.AddAsync(project, ct);
        return project;
    }

    private static void ValidateRequest(ImageGenerationRequestDto request)
    {
        if (request.NumberOfImages is < 1 or > 4)
            throw new ArgumentException("NumberOfImages deve estar entre 1 e 4.");

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
                featureType: "ImageGeneration",
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
            _logger.LogError(trackEx, "Failed to track failed usage for request {RequestId}", requestId);
        }
    }

    /// <summary>
    /// SaveChanges best-effort: falha de persistência não pode descartar uma geração
    /// que já foi paga/concluída no provider — loga alto e segue.
    /// </summary>
    private async Task<bool> TrySaveChangesAsync(string requestId, CancellationToken ct)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ImageGeneration: FALHA AO PERSISTIR no banco (RequestId: {RequestId}). " +
                "A resposta ainda será entregue ao usuário, mas o histórico não foi salvo.",
                requestId);
            return false;
        }
    }
}
