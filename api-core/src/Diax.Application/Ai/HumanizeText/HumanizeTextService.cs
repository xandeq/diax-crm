using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Ai.HumanizeText;

public class HumanizeTextService : IApplicationService, IHumanizeTextService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly ILogger<HumanizeTextService> _logger;

    public HumanizeTextService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        ILogger<HumanizeTextService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _logger = logger;
    }

    public async Task<HumanizeTextResponseDto> HumanizeAsync(HumanizeTextRequestDto request, CancellationToken ct = default)
    {
        var provider = request.Provider.ToLower();
        var client = _aiClients.FirstOrDefault(c => c.ProviderName == provider)
            ?? throw new InvalidOperationException($"Provedor '{request.Provider}' não suportado.");

        var providerSettings = GetProviderSettings(provider);
        var options = new AiClientOptions(
            ApiKey: providerSettings.ApiKey ?? string.Empty,
            BaseUrl: providerSettings.BaseUrl ?? string.Empty,
            Model: providerSettings.Model ?? string.Empty,
            Temperature: request.Temperature ?? 0.7,
            MaxTokens: request.MaxTokens
        );

        var systemPrompt = HumanizeTextPromptBuilder.BuildSystemPrompt(request.Tone, request.Language ?? "pt-BR");
        var userPrompt = HumanizeTextPromptBuilder.BuildUserPrompt(request.InputText);

        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("HumanizeText started. RequestId: {RequestId}. Provider: {Provider}. Tone: {Tone}. InputLength: {InputLength}",
            requestId, provider, request.Tone, request.InputText.Length);

        var startTime = DateTime.UtcNow;
        var outputText = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("HumanizeText completed. RequestId: {RequestId}. OutputLength: {OutputLength}. Duration: {Duration}ms",
            requestId, outputText.Length, duration.TotalMilliseconds);

        return new HumanizeTextResponseDto(
            OutputText: outputText,
            ProviderUsed: provider,
            ToneUsed: request.Tone,
            RequestId: requestId
        );
    }

    private ProviderConfig GetProviderSettings(string provider)
    {
        return provider switch
        {
            "chatgpt" => _settings.OpenAI,
            "perplexity" => _settings.Perplexity,
            "deepseek" => _settings.DeepSeek,
            _ => throw new InvalidOperationException($"Settings not found for provider {provider}")
        };
    }
}
