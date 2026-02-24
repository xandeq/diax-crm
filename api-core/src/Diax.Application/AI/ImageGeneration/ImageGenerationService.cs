using Diax.Application.AI.ImageGeneration.Dtos;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.ImageGeneration;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.ImageGeneration;

public class ImageGenerationService : IApplicationService, IImageGenerationService
{
    private readonly IEnumerable<IAiImageGenerationClient> _imageClients;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiProviderCredentialRepository _credentialRepository;
    private readonly IImageGenerationProjectRepository _projectRepository;
    private readonly IGeneratedImageRepository _generatedImageRepository;
    private readonly IImageTemplateRepository _templateRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly PromptGeneratorSettings _promptSettings;
    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        IEnumerable<IAiImageGenerationClient> imageClients,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IAiProviderCredentialRepository credentialRepository,
        IImageGenerationProjectRepository projectRepository,
        IGeneratedImageRepository generatedImageRepository,
        IImageTemplateRepository templateRepository,
        IApiKeyEncryptionService encryptionService,
        PromptGeneratorSettings promptSettings,
        ILogger<ImageGenerationService> logger)
    {
        _imageClients = imageClients;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _credentialRepository = credentialRepository;
        _projectRepository = projectRepository;
        _generatedImageRepository = generatedImageRepository;
        _templateRepository = templateRepository;
        _encryptionService = encryptionService;
        _promptSettings = promptSettings;
        _logger = logger;
    }

    public async Task<ImageGenerationResponseDto> GenerateAsync(
        ImageGenerationRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        var providerKey = request.Provider.ToLower();

        // 1. Validate provider exists and is active
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

        // 3. Validate model supports image generation
        if (!model.SupportsImageGeneration())
            throw new ArgumentException(
                $"Modelo '{request.Model}' não suporta geração de imagens. " +
                "Verifique se o CapabilitiesJson do modelo está configurado corretamente.");

        // 4. Get API key — DB credentials first, fallback to appsettings (PromptGeneratorSettings)
        string apiKey;
        var credential = await _credentialRepository.GetByProviderIdAsync(provider.Id, ct);
        if (credential != null && credential.IsConfigured())
        {
            _logger.LogDebug(
                "ImageGeneration: using DB credential for provider '{Provider}'", providerKey);
            apiKey = _encryptionService.Decrypt(credential.ApiKeyEncrypted);
        }
        else
        {
            // Fallback: read from appsettings / environment variables (same source as text services)
            var providerConfig = _promptSettings.GetProviderConfig(providerKey);
            if (providerConfig == null || string.IsNullOrWhiteSpace(providerConfig.ApiKey))
                throw new InvalidOperationException(
                    $"API Key não configurada para o provider '{providerKey}'. " +
                    "Configure a chave em Administração > AI > Providers ou nas variáveis de ambiente.");

            _logger.LogDebug(
                "ImageGeneration: DB credential not found for '{Provider}', using appsettings fallback", providerKey);
            apiKey = providerConfig.ApiKey;
        }

        // 5. Find the matching image generation client
        var client = _imageClients.FirstOrDefault(c => c.ProviderName == providerKey)
            ?? throw new InvalidOperationException(
                $"Client de geração de imagem não encontrado para '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        // 6. Create or use existing project
        ImageGenerationProject project;
        if (request.ProjectId.HasValue)
        {
            project = await _projectRepository.GetByIdAsync(request.ProjectId.Value, ct)
                ?? throw new ArgumentException($"Projeto '{request.ProjectId}' não encontrado.");
        }
        else
        {
            project = new ImageGenerationProject(
                userId: userId,
                name: $"Geração {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            await _projectRepository.AddAsync(project, ct);
        }

        // 7. Build final prompt (with template if applicable)
        string? templatePrompt = null;
        if (project.TemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(project.TemplateId.Value, ct);
            templatePrompt = template?.PromptTemplate;
        }

        var finalPrompt = ImagePromptBuilder.Build(
            request.Prompt,
            templatePrompt,
            project.ParametersJson);

        // 8. Build options and generate
        var options = new ImageGenerationOptions(
            ApiKey: apiKey,
            BaseUrl: provider.BaseUrl ?? string.Empty,
            Model: request.Model,
            Width: request.Width,
            Height: request.Height,
            NumberOfImages: request.NumberOfImages,
            NegativePrompt: request.NegativePrompt,
            Seed: request.Seed,
            Style: request.Style,
            Quality: request.Quality
        );

        var requestId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "ImageGeneration started. RequestId: {RequestId}. Provider: {Provider}. Model: {Model}. Size: {W}x{H}",
            requestId, providerKey, request.Model, request.Width, request.Height);

        project.SetProcessing();
        await _projectRepository.UpdateAsync(project, ct);

        try
        {
            var results = await client.GenerateAsync(
                finalPrompt, options, request.ReferenceImageBase64, ct);

            var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // 9. Persist generated images
            var imageEntities = new List<GeneratedImage>();
            foreach (var result in results)
            {
                var image = new GeneratedImage(
                    projectId: project.Id,
                    userId: userId,
                    providerId: provider.Id,
                    modelId: model.Id,
                    prompt: finalPrompt,
                    width: request.Width,
                    height: request.Height,
                    durationMs: durationMs,
                    success: true,
                    revisedPrompt: result.RevisedPrompt,
                    providerUrl: result.IsBase64 ? null : result.ImageUrl,
                    storageUrl: result.ImageUrl,
                    seed: result.Seed);

                await _generatedImageRepository.AddAsync(image, ct);
                imageEntities.Add(image);
            }

            project.SetCompleted();
            await _projectRepository.UpdateAsync(project, ct);

            _logger.LogInformation(
                "ImageGeneration completed. RequestId: {RequestId}. Images: {Count}. Duration: {Duration}ms",
                requestId, results.Count, durationMs);

            // 10. Track usage (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _usageTracking.LogUsageAsync(
                        userId: userId,
                        providerId: provider.Id,
                        modelId: model.Id,
                        featureType: "ImageGeneration",
                        duration: TimeSpan.FromMilliseconds(durationMs),
                        success: true,
                        requestId: requestId,
                        inputTokens: finalPrompt.Length,
                        outputTokens: results.Count,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to track usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            return new ImageGenerationResponseDto(
                ProjectId: project.Id,
                ProviderUsed: providerKey,
                ModelUsed: request.Model,
                RequestId: requestId,
                DurationMs: durationMs,
                Images: imageEntities.Select(i => new GeneratedImageDto(
                    Id: i.Id,
                    ImageUrl: i.StorageUrl ?? i.ProviderUrl ?? string.Empty,
                    RevisedPrompt: i.RevisedPrompt,
                    Seed: i.Seed,
                    Width: i.Width,
                    Height: i.Height
                )).ToList()
            );
        }
        catch (Exception ex)
        {
            var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            project.SetFailed();
            await _projectRepository.UpdateAsync(project, ct);

            // Track failed usage
            _ = Task.Run(async () =>
            {
                try
                {
                    await _usageTracking.LogUsageAsync(
                        userId: userId,
                        providerId: provider.Id,
                        modelId: model.Id,
                        featureType: "ImageGeneration",
                        duration: TimeSpan.FromMilliseconds(durationMs),
                        success: false,
                        requestId: requestId,
                        errorMessage: ex.Message,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception trackEx)
                {
                    _logger.LogError(trackEx, "Failed to track failed usage for request {RequestId}", requestId);
                }
            }, CancellationToken.None);

            _logger.LogError(ex, "ImageGeneration failed. RequestId: {RequestId}", requestId);
            throw;
        }
    }
}
