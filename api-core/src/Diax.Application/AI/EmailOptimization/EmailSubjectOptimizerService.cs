using Diax.Application.AI;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Diax.Application.AI.EmailOptimization;

public class EmailSubjectOptimizerService : IApplicationService, IEmailSubjectOptimizerService
{
    private readonly IEnumerable<IAiTextTransformClient> _aiClients;
    private readonly PromptGeneratorSettings _settings;
    private readonly IAiModelValidator _aiModelValidator;
    private readonly IAiUsageTrackingService _usageTracking;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<EmailSubjectOptimizerService> _logger;

    public EmailSubjectOptimizerService(
        IEnumerable<IAiTextTransformClient> aiClients,
        PromptGeneratorSettings settings,
        IAiModelValidator aiModelValidator,
        IAiUsageTrackingService usageTracking,
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        ILogger<EmailSubjectOptimizerService> logger)
    {
        _aiClients = aiClients;
        _settings = settings;
        _aiModelValidator = aiModelValidator;
        _usageTracking = usageTracking;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<GenerateSubjectLinesResponseDto> GenerateSubjectLinesAsync(
        GenerateSubjectLinesRequestDto request,
        Guid userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BaseMessage))
            throw new ArgumentException("BaseMessage é obrigatório.");

        var providerKey = request.Provider.ToLower();

        // Validação do provider contra o banco de dados
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

        // Construir prompt
        var systemPrompt = "Você é um especialista em email marketing e copywriting. Gere 5 subject lines otimizadas para email com alto potencial de abertura. Retorne como JSON puro, sem markdown ou explicação.";

        var audienceContext = !string.IsNullOrWhiteSpace(request.TargetAudience)
            ? $"Público-alvo: {request.TargetAudience}\n"
            : "";

        var userPrompt = $@"Mensagem base: {request.BaseMessage}
{audienceContext}
Requisitos:
- Cada subject line deve ter no máximo 50 caracteres
- Para cada linha, inclua: text (string), angle (string descrevendo a técnica/ângulo), estimatedOpenRate (número entre 0 e 1)

Formato JSON esperado:
{{
  ""subjects"": [
    {{""text"": ""exemplo"", ""angle"": ""urgência"", ""estimatedOpenRate"": 0.92}},
    {{""text"": ""outro"", ""angle"": ""curiosidade"", ""estimatedOpenRate"": 0.88}}
  ]
}}

Gere agora os 5 subject lines otimizados:";

        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("Email subject optimization started. RequestId: {RequestId}. Provider: {Provider}. BaseMessageLength: {Length}",
            requestId, providerKey, request.BaseMessage.Length);

        var startTime = DateTime.UtcNow;
        string? response = null;
        bool success = false;
        string? errorMsg = null;

        try
        {
            response = await client.TransformAsync(systemPrompt, userPrompt, options, ct);
            success = true;

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Email subject optimization completed. RequestId: {RequestId}. Duration: {Duration}ms",
                requestId, duration.TotalMilliseconds);

            // Parsear resposta
            var subjectLines = ParseSubjectLinesFromResponse(response);

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
                                featureType: "EmailSubjectOptimization",
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

            var result = new GenerateSubjectLinesResponseDto(
                SubjectLines: subjectLines,
                ProviderUsed: providerKey,
                ModelUsed: options.Model,
                GeneratedAt: DateTime.UtcNow,
                RequestId: requestId);

            return result;
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
                                featureType: "EmailSubjectOptimization",
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

    private List<SubjectLineDto> ParseSubjectLinesFromResponse(string response)
    {
        var result = new List<SubjectLineDto>();

        try
        {
            var jsonContent = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("subjects", out var subjectsArray) &&
                subjectsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in subjectsArray.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var textEl) &&
                        item.TryGetProperty("angle", out var angleEl) &&
                        item.TryGetProperty("estimatedOpenRate", out var rateEl))
                    {
                        var text = textEl.GetString();
                        var angle = angleEl.GetString();
                        var rate = rateEl.TryGetDecimal(out var decimalRate) ? decimalRate : 0.5m;

                        if (!string.IsNullOrWhiteSpace(text))
                            result.Add(new SubjectLineDto(text, angle ?? "N/A", rate));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON parsing error: {Error}", ex.Message);
        }

        if (result.Count == 0)
            result.Add(new SubjectLineDto("Unable to generate", "fallback", 0.5m));

        return result;
    }
}
