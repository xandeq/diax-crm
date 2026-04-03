using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Application.AI.SocialMediaBatch;

public class SocialMediaBatchService : IApplicationService, ISocialMediaBatchService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<SocialMediaBatchService> _logger;

    public SocialMediaBatchService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ILogger<SocialMediaBatchService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<GenerateSocialBatchResponseDto> GenerateBatchAsync(
        GenerateSocialBatchRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        if (request.Topics == null || request.Topics.Count == 0)
            throw new ArgumentException("Pelo menos um tópico é obrigatório.");
        if (request.Platforms == null || request.Platforms.Count == 0)
            throw new ArgumentException("Pelo menos uma plataforma é obrigatória.");

        var providerKey = request.Provider.ToLower();

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

        var providerSettings = _settings.GetProviderConfig(providerKey)
            ?? throw new InvalidOperationException(
                $"Configuração não encontrada para o provider '{providerKey}'.");

        if (string.IsNullOrWhiteSpace(providerSettings.ApiKey))
            throw new InvalidOperationException($"API Key não configurada para o provider '{providerKey}'.");

        var client = _aiClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Client de IA não encontrado para o provider '{providerKey}'.");

        var options = new AiClientOptions(
            ApiKey: providerSettings.ApiKey,
            BaseUrl: providerSettings.BaseUrl ?? string.Empty,
            Model: !string.IsNullOrWhiteSpace(request.Model) ? request.Model : (providerSettings.Model ?? string.Empty),
            Temperature: request.Temperature ?? 0.8,
            MaxTokens: request.MaxTokens ?? 4000
        );

        var systemPrompt = @"Você é um social media manager experiente especializado em marketing digital brasileiro.
Gere posts otimizados para redes sociais com copy persuasivo, hashtags relevantes e sugestões de imagem.
Retorne como JSON puro, sem markdown ou explicação.";

        var topicsStr = string.Join(", ", request.Topics);
        var platformsStr = string.Join(", ", request.Platforms);
        var monthContext = !string.IsNullOrWhiteSpace(request.Month) ? $"Mês de referência: {request.Month}\n" : "";
        var brandContext = !string.IsNullOrWhiteSpace(request.BrandVoice) ? $"Tom da marca: {request.BrandVoice}\n" : "";
        var audienceContext = !string.IsNullOrWhiteSpace(request.TargetAudience) ? $"Público-alvo: {request.TargetAudience}\n" : "";

        var postCount = Math.Clamp(request.PostCount, 3, 30);

        var userPrompt = $@"Tópicos: {topicsStr}
Plataformas: {platformsStr}
{monthContext}{brandContext}{audienceContext}
Requisitos:
- Gere exatamente {postCount} posts distribuídos entre as plataformas
- Para cada post inclua: number (int), platform (string), contentType (""carrossel"", ""post_imagem"", ""reels"", ""stories"", ""texto""), caption (string com quebras de linha), hashtags (array de strings sem #), imagePrompt (prompt para gerar imagem, ou null se texto puro), imageDimension (""1080x1080"" para feed, ""1080x1920"" para stories/reels, ""1200x628"" para linkedin), bestTimeToPost (ex: ""terça 10h""), topic (qual tópico abrange)

Formato JSON esperado:
{{
  ""posts"": [
    {{
      ""number"": 1,
      ""platform"": ""instagram"",
      ""contentType"": ""carrossel"",
      ""caption"": ""Título impactante...\n\nCorpo do post...\n\nCTA"",
      ""hashtags"": [""marketing"", ""dica""],
      ""imagePrompt"": ""Professional flat design illustration of..."",
      ""imageDimension"": ""1080x1080"",
      ""bestTimeToPost"": ""terça 10h"",
      ""topic"": ""marketing digital""
    }}
  ]
}}

Gere agora os {postCount} posts:";

        var requestId = Guid.NewGuid().ToString();
        _logger.LogInformation("Social media batch started. RequestId: {RequestId}. Provider: {Provider}. Posts: {Count}",
            requestId, providerKey, postCount);

        var startTime = DateTime.UtcNow;

        try
        {
            var response = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Social media batch completed. RequestId: {RequestId}. Duration: {Duration}ms",
                requestId, duration.TotalMilliseconds);

            var posts = ParsePostsFromResponse(response);

            TrackUsageFireAndForget(userId, providerKey, request.Model, requestId, duration, true, null);

            return new GenerateSocialBatchResponseDto(
                Posts: posts,
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
                            featureType: "SocialMediaBatch",
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

    private List<SocialPostDto> ParsePostsFromResponse(string response)
    {
        var result = new List<SocialPostDto>();

        try
        {
            var jsonContent = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("posts", out var postsArray) &&
                postsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in postsArray.EnumerateArray())
                {
                    var number = item.TryGetProperty("number", out var numEl) && numEl.TryGetInt32(out var n) ? n : result.Count + 1;
                    var platform = item.TryGetProperty("platform", out var platEl) ? platEl.GetString() ?? "" : "";
                    var contentType = item.TryGetProperty("contentType", out var ctEl) ? ctEl.GetString() ?? "post_imagem" : "post_imagem";
                    var caption = item.TryGetProperty("caption", out var capEl) ? capEl.GetString() ?? "" : "";
                    var imagePrompt = item.TryGetProperty("imagePrompt", out var imgEl) && imgEl.ValueKind != JsonValueKind.Null ? imgEl.GetString() : null;
                    var imageDimension = item.TryGetProperty("imageDimension", out var dimEl) ? dimEl.GetString() ?? "1080x1080" : "1080x1080";
                    var bestTime = item.TryGetProperty("bestTimeToPost", out var timeEl) ? timeEl.GetString() ?? "" : "";
                    var topic = item.TryGetProperty("topic", out var topicEl) ? topicEl.GetString() ?? "" : "";

                    var hashtags = new List<string>();
                    if (item.TryGetProperty("hashtags", out var hashEl) && hashEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var h in hashEl.EnumerateArray())
                        {
                            var tag = h.GetString();
                            if (!string.IsNullOrWhiteSpace(tag))
                                hashtags.Add(tag.TrimStart('#'));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(caption))
                        result.Add(new SocialPostDto(number, platform, contentType, caption, hashtags, imagePrompt, imageDimension, bestTime, topic));
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON parsing error in social batch response: {Error}", ex.Message);
        }

        if (result.Count == 0)
            result.Add(new SocialPostDto(1, "instagram", "post_imagem", "Não foi possível gerar posts.", new List<string>(), null, "1080x1080", "", "erro"));

        return result;
    }
}
