using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.FreeGateway;

/// <summary>
/// Gateway de IAs GRATUITAS: percorre uma cadeia de provedores OpenAI-compatíveis até obter
/// sucesso, pulando provedores sem chave configurada ou em cooldown (429/402 recente).
///
/// Todos os provedores suportados expõem POST {base}/chat/completions com Bearer token, no
/// mesmo formato da OpenAI — só a base URL muda.
/// </summary>
public class FreeAiGatewayService : IApplicationService, IFreeAiGatewayService
{
    private const int MaxMessages = 50;
    private const int MaxTotalContentChars = 60_000;
    private const int DefaultMaxTokens = 2048;
    private const int MinMaxTokens = 1;
    private const int MaxMaxTokens = 8192;
    private const double MinTemperature = 0;
    private const double MaxTemperature = 2;
    private const int MaxHealthConcurrency = 4;

    private const string HealthCacheKey = "FreeAiGateway:Health";
    private static readonly TimeSpan HealthCacheDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HealthMinRefreshInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Serializa refresh de /health entre requisições concorrentes. Estático de propósito: o
    /// serviço é Scoped (nova instância por requisição), mas o cache (IMemoryCache) e o lock
    /// precisam ser únicos no processo para não martelar os provedores em paralelo.
    /// </summary>
    private static readonly SemaphoreSlim HealthRefreshLock = new(1, 1);

    private static readonly string[] ValidRoles = { "system", "user", "assistant" };

    /// <summary>
    /// Cadeia padrão usada quando a config "FreeAi:Chain" vier vazia. Validada ao vivo em 27/10/2026
    /// (gemini e groq responderam; openrouter/cerebras/huggingface dependem da chave de produção).
    /// Ordem: qualidade para escrever prompts primeiro, provedores intercalados para que o 429 de um
    /// não derrube os seguintes. GET free-ai/v1/health mostra o estado real de cada item.
    /// </summary>
    public static readonly IReadOnlyList<FreeAiChainEntry> DefaultChain = new List<FreeAiChainEntry>
    {
        new("gemini", "gemini-2.5-flash"),
        new("groq", "openai/gpt-oss-120b"),
        new("openrouter", "google/gemma-4-31b-it:free"),
        new("groq", "qwen/qwen3.8-27b"),
        new("gemini", "gemini-2.5-flash-lite"),
        new("cerebras", "gpt-oss-120b"),
        new("openrouter", "nvidia/nemotron-3-ultra-550b-a55b:free"),
        new("huggingface", "Qwen/Qwen3.8-27B"),
        new("groq", "allam-2-7b"),
    };

    /// <summary>Base URL OpenAI-compatível por provedor. Único universo suportado pelo gateway.</summary>
    public static readonly IReadOnlyDictionary<string, string> BaseUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["groq"] = "https://api.groq.com/openai/v1",
        ["cerebras"] = "https://api.cerebras.ai/v1",
        ["gemini"] = "https://generativelanguage.googleapis.com/v1beta/openai",
        ["openrouter"] = "https://openrouter.ai/api/v1",
        ["huggingface"] = "https://router.huggingface.co/v1",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiProviderCredentialRepository _credentialRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly PromptGeneratorSettings _promptSettings;
    private readonly IFreeAiCooldownStore _cooldownStore;
    private readonly FreeAiOptions _options;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<FreeAiGatewayService> _logger;

    private readonly List<FreeAiChainEntry> _effectiveChain;

    public FreeAiGatewayService(
        IHttpClientFactory httpClientFactory,
        IAiProviderRepository providerRepository,
        IAiProviderCredentialRepository credentialRepository,
        IApiKeyEncryptionService encryptionService,
        PromptGeneratorSettings promptSettings,
        IFreeAiCooldownStore cooldownStore,
        FreeAiOptions options,
        IMemoryCache memoryCache,
        ILogger<FreeAiGatewayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _providerRepository = providerRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _promptSettings = promptSettings;
        _cooldownStore = cooldownStore;
        _options = options;
        _memoryCache = memoryCache;
        _logger = logger;

        var source = _options.Chain is { Count: > 0 } ? _options.Chain : DefaultChain;
        _effectiveChain = new List<FreeAiChainEntry>();
        foreach (var entry in source)
        {
            if (!BaseUrls.ContainsKey(entry.Provider))
            {
                _logger.LogWarning("FreeAiGateway: provedor '{Provider}' na config não é suportado (só groq/cerebras/gemini/openrouter/huggingface) — ignorado.", entry.Provider);
                continue;
            }

            _effectiveChain.Add(entry);
        }
    }

    public IReadOnlyList<FreeAiChainEntry> GetChain() => _effectiveChain;

    public async Task<FreeAiChatResult> CompleteAsync(FreeAiChatRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var orderedChain = BuildOrderedChain(request.Model);
        var attempts = new List<FreeAiAttempt>();

        foreach (var entry in orderedChain)
        {
            if (_cooldownStore.IsInCooldown(entry.Provider))
            {
                attempts.Add(new FreeAiAttempt { Provider = entry.Provider, Model = entry.Model, Status = "cooldown" });
                continue;
            }

            var apiKey = await TryResolveApiKeyAsync(entry.Provider, cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                attempts.Add(new FreeAiAttempt { Provider = entry.Provider, Model = entry.Model, Status = "no_api_key" });
                continue;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var (content, finishReason, promptTokens, completionTokens, statusCode) =
                    await CallProviderAsync(entry, apiKey, request, cancellationToken);

                stopwatch.Stop();

                if (statusCode is 429 or 402)
                    _cooldownStore.SetCooldown(entry.Provider, TimeSpan.FromSeconds(_options.CooldownSeconds));

                if (statusCode is < 200 or >= 300)
                {
                    attempts.Add(new FreeAiAttempt
                    {
                        Provider = entry.Provider,
                        Model = entry.Model,
                        Status = statusCode.ToString(),
                        Error = $"HTTP {statusCode}",
                        ElapsedMs = stopwatch.ElapsedMilliseconds,
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    attempts.Add(new FreeAiAttempt
                    {
                        Provider = entry.Provider,
                        Model = entry.Model,
                        Status = "empty_content",
                        ElapsedMs = stopwatch.ElapsedMilliseconds,
                    });
                    continue;
                }

                attempts.Add(new FreeAiAttempt
                {
                    Provider = entry.Provider,
                    Model = entry.Model,
                    Status = "ok",
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                });

                _logger.LogInformation(
                    "FreeAiGateway: sucesso via {Provider}/{Model} em {Ms}ms (mensagens={MessageCount}, tentativas={AttemptCount})",
                    entry.Provider, entry.Model, stopwatch.ElapsedMilliseconds, request.Messages.Count, attempts.Count);

                return new FreeAiChatResult
                {
                    Success = true,
                    Content = content,
                    Provider = entry.Provider,
                    Model = entry.Model,
                    FinishReason = finishReason,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    Attempts = attempts,
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                attempts.Add(new FreeAiAttempt { Provider = entry.Provider, Model = entry.Model, Status = "timeout", ElapsedMs = stopwatch.ElapsedMilliseconds });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                attempts.Add(new FreeAiAttempt
                {
                    Provider = entry.Provider,
                    Model = entry.Model,
                    Status = "error",
                    Error = ex.GetType().Name,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                });
                _logger.LogWarning(ex, "FreeAiGateway: falha ao chamar {Provider}/{Model}, tentando próximo.", entry.Provider, entry.Model);
            }
        }

        _logger.LogWarning("FreeAiGateway: todos os {Count} provedores da cadeia falharam.", attempts.Count);

        return new FreeAiChatResult
        {
            Success = false,
            Attempts = attempts,
        };
    }

    public async Task<FreeAiHealthResult> CheckHealthAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (TryGetCachedHealth(forceRefresh, out var cached))
            return cached!;

        await HealthRefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Outra requisição pode ter atualizado o cache enquanto esperávamos o lock.
            if (TryGetCachedHealth(forceRefresh, out var cachedAfterLock))
                return cachedAfterLock!;

            var fresh = await RunHealthChecksAsync(cancellationToken);
            fresh.CheckedAt = DateTimeOffset.UtcNow;
            fresh.Cached = false;
            _memoryCache.Set(HealthCacheKey, fresh, HealthCacheDuration);
            return fresh;
        }
        finally
        {
            HealthRefreshLock.Release();
        }
    }

    /// <summary>
    /// Tenta devolver o health direto do cache. forceRefresh só é respeitado se o último check
    /// tiver mais de <see cref="HealthMinRefreshInterval"/> — caso contrário serve o cache mesmo
    /// com refresh pedido, para impedir que alguém martele os provedores via query string.
    /// </summary>
    private bool TryGetCachedHealth(bool forceRefresh, out FreeAiHealthResult? result)
    {
        if (!_memoryCache.TryGetValue(HealthCacheKey, out FreeAiHealthResult? cached) || cached is null)
        {
            result = null;
            return false;
        }

        var age = DateTimeOffset.UtcNow - cached.CheckedAt;
        if (forceRefresh && age >= HealthMinRefreshInterval)
        {
            result = null;
            return false;
        }

        result = new FreeAiHealthResult { Items = cached.Items, CheckedAt = cached.CheckedAt, Cached = true };
        return true;
    }

    private async Task<FreeAiHealthResult> RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        // Chaves resolvidas em sequência ANTES do paralelo: os repositórios usam o DbContext do
        // request, que não aceita duas operações ao mesmo tempo. Só a chamada HTTP roda em paralelo.
        var keys = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in _effectiveChain.Select(e => e.Provider).Distinct(StringComparer.OrdinalIgnoreCase))
            keys[provider] = await TryResolveApiKeyAsync(provider, cancellationToken);

        var semaphore = new SemaphoreSlim(MaxHealthConcurrency);
        var tasks = _effectiveChain.Select(async entry =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await CheckOneHealthAsync(entry, keys[entry.Provider], cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var items = await Task.WhenAll(tasks);
        return new FreeAiHealthResult { Items = items.ToList() };
    }

    private async Task<FreeAiHealthItem> CheckOneHealthAsync(FreeAiChainEntry entry, string? apiKey, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            stopwatch.Stop();
            return new FreeAiHealthItem { Provider = entry.Provider, Model = entry.Model, Ok = false, Error = "no_api_key", ElapsedMs = stopwatch.ElapsedMilliseconds };
        }

        var pingRequest = new FreeAiChatRequest
        {
            Messages = new List<FreeAiChatMessage> { new() { Role = "user", Content = "oi" } },
            MaxTokens = 256, // modelos de raciocínio (gpt-oss) gastam tokens pensando antes de responder
        };

        try
        {
            var (content, _, _, _, statusCode) = await CallProviderAsync(entry, apiKey, pingRequest, cancellationToken);
            stopwatch.Stop();

            if (statusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(content))
                return new FreeAiHealthItem { Provider = entry.Provider, Model = entry.Model, Ok = true, ElapsedMs = stopwatch.ElapsedMilliseconds };

            return new FreeAiHealthItem { Provider = entry.Provider, Model = entry.Model, Ok = false, Error = $"HTTP {statusCode}", ElapsedMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new FreeAiHealthItem { Provider = entry.Provider, Model = entry.Model, Ok = false, Error = ex.GetType().Name, ElapsedMs = stopwatch.ElapsedMilliseconds };
        }
    }

    /// <summary>
    /// Monta a cadeia a tentar a partir do parâmetro "model" da requisição.
    /// null/"auto" -> cadeia inteira. "provider/model" ou "model" sozinho -> precisa casar
    /// (provider+model, ou só model, case-insensitive) com um item JÁ EXISTENTE na cadeia
    /// configurada; começa por ele e cai para o resto.
    ///
    /// Importante por custo: NÃO aceitamos um par "provider/model" arbitrário fora da cadeia.
    /// Um cliente mandando "openrouter/openai/gpt-4o" (modelo pago) usaria a chave de produção da
    /// OpenRouter para um modelo caríssimo se isso fosse permitido — por isso o modelo precisa
    /// estar na lista que este servidor decidiu (GET /free-ai/v1/models).
    /// </summary>
    private List<FreeAiChainEntry> BuildOrderedChain(string? requestedModel)
    {
        if (string.IsNullOrWhiteSpace(requestedModel) || requestedModel.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return new List<FreeAiChainEntry>(_effectiveChain);

        var slashIndex = requestedModel.IndexOf('/');
        FreeAiChainEntry? match;
        if (slashIndex > 0)
        {
            var provider = requestedModel[..slashIndex];
            var model = requestedModel[(slashIndex + 1)..];
            match = _effectiveChain.FirstOrDefault(e =>
                e.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) && e.Model.Equals(model, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            match = _effectiveChain.FirstOrDefault(e => e.Model.Equals(requestedModel, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null)
            throw new ArgumentException(
                $"Model '{requestedModel}' não existe na cadeia configurada. Use GET free-ai/v1/models para ver as opções disponíveis, ou 'auto'.");

        var ordered = new List<FreeAiChainEntry> { match };
        foreach (var entry in _effectiveChain)
        {
            if (entry.Provider.Equals(match.Provider, StringComparison.OrdinalIgnoreCase) && entry.Model.Equals(match.Model, StringComparison.OrdinalIgnoreCase))
                continue;

            ordered.Add(entry);
        }

        return ordered;
    }

    private static void Validate(FreeAiChatRequest request)
    {
        if (request.Messages is null || request.Messages.Count == 0)
            throw new ArgumentException("messages precisa ter pelo menos 1 item.");

        if (request.Messages.Count > MaxMessages)
            throw new ArgumentException($"messages excede o máximo de {MaxMessages} itens.");

        var totalChars = 0;
        foreach (var message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role) || !ValidRoles.Contains(message.Role, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"role inválida: '{message.Role}'. Use system, user ou assistant.");

            totalChars += message.Content?.Length ?? 0;
        }

        if (totalChars > MaxTotalContentChars)
            throw new ArgumentException($"Conteúdo total das mensagens excede {MaxTotalContentChars} caracteres.");

        if (request.MaxTokens is { } maxTokens && (maxTokens < MinMaxTokens || maxTokens > MaxMaxTokens))
            throw new ArgumentException($"max_tokens precisa estar entre {MinMaxTokens} e {MaxMaxTokens}.");

        if (request.Temperature is { } temperature && (temperature < MinTemperature || temperature > MaxTemperature))
            throw new ArgumentException($"temperature precisa estar entre {MinTemperature} e {MaxTemperature}.");
    }

    private async Task<string?> TryResolveApiKeyAsync(string providerKey, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByKeyAsync(providerKey, cancellationToken);
        if (provider != null)
        {
            var credential = await _credentialRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
            if (credential != null && credential.IsConfigured())
            {
                try
                {
                    return _encryptionService.Decrypt(credential.ApiKeyEncrypted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FreeAiGateway: falha ao descriptografar credencial de '{Provider}', tentando appsettings.", providerKey);
                }
            }
        }

        var providerConfig = _promptSettings.GetProviderConfig(providerKey);
        return string.IsNullOrWhiteSpace(providerConfig?.ApiKey) ? null : providerConfig.ApiKey;
    }

    private async Task<(string? Content, string? FinishReason, int? PromptTokens, int? CompletionTokens, int StatusCode)> CallProviderAsync(
        FreeAiChainEntry entry, string apiKey, FreeAiChatRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = BaseUrls[entry.Provider];
        var httpClient = _httpClientFactory.CreateClient("FreeAiGateway");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = entry.Model,
            ["messages"] = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
            ["max_tokens"] = request.MaxTokens ?? DefaultMaxTokens,
        };
        if (request.Temperature is { } temperature)
            payload["temperature"] = temperature;

        requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        using var response = await httpClient.SendAsync(requestMessage, linkedCts.Token);
        var body = await response.Content.ReadAsStringAsync(linkedCts.Token);

        if (!response.IsSuccessStatusCode)
            return (null, null, null, null, (int)response.StatusCode);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? content = null;
            string? finishReason = null;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentProp))
                    content = contentProp.GetString();
                if (firstChoice.TryGetProperty("finish_reason", out var finishProp))
                    finishReason = finishProp.GetString();
            }

            int? promptTokens = null;
            int? completionTokens = null;
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var ptVal))
                    promptTokens = ptVal;
                if (usage.TryGetProperty("completion_tokens", out var ct) && ct.TryGetInt32(out var ctVal))
                    completionTokens = ctVal;
            }

            return (content, finishReason, promptTokens, completionTokens, (int)response.StatusCode);
        }
        catch (JsonException)
        {
            return (null, null, null, null, (int)response.StatusCode);
        }
    }
}
