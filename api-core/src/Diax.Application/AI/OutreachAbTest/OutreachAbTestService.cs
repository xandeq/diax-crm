using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Application.AI.OutreachAbTest;

public class OutreachAbTestService : IApplicationService, IOutreachAbTestService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<OutreachAbTestService> _logger;

    public OutreachAbTestService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ILogger<OutreachAbTestService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<GenerateAbVariationsResponseDto> GenerateVariationsAsync(
        GenerateAbVariationsRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BaseMessage))
            throw new ArgumentException("BaseMessage é obrigatório.");

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
                $"Configuração não encontrada para o provider '{providerKey}'. " +
                "Verifique se o appsettings contém a seção PromptGenerator com as credenciais do provider.");

        if (string.IsNullOrWhiteSpace(providerSettings.ApiKey))
        {
            throw new InvalidOperationException(
                $"API Key não configurada para o provider '{providerKey}'. " +
                "Configure a chave em appsettings.json ou variáveis de ambiente.");
        }

        var client = _aiClients.FirstOrDefault(c => c.ProviderName.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Client de IA não encontrado para o provider '{providerKey}'. " +
                "Verifique se o client está registrado no container de DI.");

        var options = new AiClientOptions(
            ApiKey: providerSettings.ApiKey,
            BaseUrl: providerSettings.BaseUrl ?? string.Empty,
            Model: !string.IsNullOrWhiteSpace(request.Model) ? request.Model : (providerSettings.Model ?? string.Empty),
            Temperature: request.Temperature ?? 0.8,
            MaxTokens: request.MaxTokens
        );

        var systemPrompt = @"Você é um especialista em outreach, copywriting e comunicação persuasiva para negócios B2B e B2C.
Gere 3 variações de mensagem de outreach (A/B/C test) com tons diferentes para maximizar a taxa de resposta.
Cada variação deve ter um subject line de email e um body completo.
Retorne como JSON puro, sem markdown ou explicação.";

        var audienceContext = !string.IsNullOrWhiteSpace(request.TargetAudience)
            ? $"Público-alvo: {request.TargetAudience}\n" : "";
        var industryContext = !string.IsNullOrWhiteSpace(request.Industry)
            ? $"Indústria/Nicho: {request.Industry}\n" : "";
        var goalContext = !string.IsNullOrWhiteSpace(request.Goal)
            ? $"Objetivo: {request.Goal}\n" : "";

        var userPrompt = $@"Mensagem base / oferta: {request.BaseMessage}
{audienceContext}{industryContext}{goalContext}
Requisitos:
- Gere exatamente 3 variações com tons diferentes: ""profissional"" (formal e direto), ""casual"" (amigável e conversacional), ""urgente"" (escassez e FOMO)
- Cada variação inclui: label (""A"", ""B"", ""C""), tone (string), subject (max 60 chars), body (mensagem completa), estimatedResponseRate (0 a 1), rationale (por que essa abordagem funciona)
- O body deve ter entre 3-6 parágrafos, usar {{{{FirstName}}}} como variável de personalização
- Inclua call-to-action claro no final de cada body

Formato JSON esperado:
{{
  ""variations"": [
    {{
      ""label"": ""A"",
      ""tone"": ""profissional"",
      ""subject"": ""Proposta de parceria..."",
      ""body"": ""Olá {{{{FirstName}}}},\n\n..."",
      ""estimatedResponseRate"": 0.12,
      ""rationale"": ""Tom formal gera confiança em decisores C-level""
    }}
  ]
}}

Gere agora as 3 variações:";

        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("Outreach A/B test started. RequestId: {RequestId}. Provider: {Provider}.",
            requestId, providerKey);

        var startTime = DateTime.UtcNow;

        try
        {
            var response = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Outreach A/B test completed. RequestId: {RequestId}. Duration: {Duration}ms",
                requestId, duration.TotalMilliseconds);

            var variations = ParseVariationsFromResponse(response);

            TrackUsageFireAndForget(userId, providerKey, request.Model, requestId, duration, true, null);

            return new GenerateAbVariationsResponseDto(
                Variations: variations,
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
                            featureType: "OutreachAbTest",
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

    private List<OutreachVariationDto> ParseVariationsFromResponse(string response)
    {
        var result = new List<OutreachVariationDto>();

        try
        {
            var jsonContent = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("variations", out var variationsArray) &&
                variationsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in variationsArray.EnumerateArray())
                {
                    var label = item.TryGetProperty("label", out var labelEl) ? labelEl.GetString() ?? "?" : "?";
                    var tone = item.TryGetProperty("tone", out var toneEl) ? toneEl.GetString() ?? "" : "";
                    var subject = item.TryGetProperty("subject", out var subjectEl) ? subjectEl.GetString() ?? "" : "";
                    var body = item.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";
                    var rate = item.TryGetProperty("estimatedResponseRate", out var rateEl) && rateEl.TryGetDecimal(out var d) ? d : 0.1m;
                    var rationale = item.TryGetProperty("rationale", out var rationaleEl) ? rationaleEl.GetString() ?? "" : "";

                    if (!string.IsNullOrWhiteSpace(subject) || !string.IsNullOrWhiteSpace(body))
                        result.Add(new OutreachVariationDto(label, tone, subject, body, rate, rationale));
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON parsing error in A/B test response: {Error}", ex.Message);
        }

        if (result.Count == 0)
            result.Add(new OutreachVariationDto("A", "fallback", "Sem resultado", "Não foi possível gerar variações.", 0m, "Erro na geração"));

        return result;
    }
}
