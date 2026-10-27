using System.Net;
using System.Text;
using System.Text.Json;
using Diax.Application.AI.FreeGateway;
using Diax.Application.AI.Services;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Diax.Tests.Application.AI.FreeGateway;

public class FreeAiGatewayServiceTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static PromptGeneratorSettings BuildSettings(
        string? groqKey = "groq-key",
        string? cerebrasKey = "cerebras-key",
        string? geminiKey = "gemini-key")
    {
        return new PromptGeneratorSettings
        {
            Groq = new ProviderConfig { ApiKey = groqKey },
            Cerebras = new ProviderConfig { ApiKey = cerebrasKey },
            Gemini = new ProviderConfig { ApiKey = geminiKey },
        };
    }

    private static FreeAiOptions BuildOptions(params (string Provider, string Model)[] chain)
    {
        return new FreeAiOptions
        {
            TimeoutSeconds = 5,
            CooldownSeconds = 60,
            Chain = chain.Select(c => new FreeAiChainEntry(c.Provider, c.Model)).ToList(),
        };
    }

    private static IHttpClientFactory BuildHttpClientFactory(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => queue.Count > 0 ? queue.Dequeue() : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static HttpResponseMessage SuccessResponse(string content = "resposta gerada", string finishReason = "stop") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { role = "assistant", content }, finish_reason = finishReason } },
                    usage = new { prompt_tokens = 10, completion_tokens = 5 },
                }),
                Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage EmptyContentResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content = "" } } } }),
                Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode code) => new(code)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { error = "boom" }), Encoding.UTF8, "application/json"),
    };

    /// <summary>Factory com histórico das requisições feitas — para provar "0 chamadas" ou "1x por item".</summary>
    private static (IHttpClientFactory Factory, List<HttpRequestMessage> Requests) BuildTrackedHttpClientFactory(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        var requests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => requests.Add(req))
            .ReturnsAsync(() => queue.Count > 0 ? queue.Dequeue() : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return (factory.Object, requests);
    }

    private static FreeAiGatewayService BuildService(
        FreeAiOptions options,
        PromptGeneratorSettings settings,
        IHttpClientFactory httpClientFactory,
        IFreeAiCooldownStore? cooldownStore = null,
        IMemoryCache? memoryCache = null,
        Mock<IAiProviderRepository>? providerRepoOverride = null)
    {
        var providerRepo = providerRepoOverride ?? new Mock<IAiProviderRepository>();
        if (providerRepoOverride == null)
            providerRepo.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AiProvider?)null);

        var credentialRepo = new Mock<IAiProviderCredentialRepository>();
        var encryptionService = new Mock<IApiKeyEncryptionService>();

        return new FreeAiGatewayService(
            httpClientFactory,
            providerRepo.Object,
            credentialRepo.Object,
            encryptionService.Object,
            settings,
            cooldownStore ?? new FreeAiCooldownStore(),
            options,
            memoryCache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<FreeAiGatewayService>.Instance);
    }

    private static FreeAiChatRequest BuildRequest(string? model = null) => new()
    {
        Messages = new List<FreeAiChatMessage> { new() { Role = "user", Content = "oi" } },
        Model = model,
    };

    // ─── Sucesso no primeiro provedor ───────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_FirstProviderSucceeds_ReturnsItsContent()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(SuccessResponse("ola do groq"));
        var service = BuildService(options, BuildSettings(), factory);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("ola do groq", result.Content);
        Assert.Equal("groq", result.Provider);
        Assert.Equal("model-a", result.Model);
        Assert.Single(result.Attempts);
        Assert.Equal("ok", result.Attempts[0].Status);
    }

    // ─── 429 no primeiro -> usa o segundo, e o primeiro entra em cooldown ───────

    [Fact]
    public async Task CompleteAsync_FirstProviderRateLimited_FallsBackAndCoolsDown()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var cooldownStore = new FreeAiCooldownStore();
        var factory = BuildHttpClientFactory(ErrorResponse(HttpStatusCode.TooManyRequests), SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory, cooldownStore);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal(2, result.Attempts.Count);
        Assert.Equal("429", result.Attempts[0].Status);
        Assert.Equal("ok", result.Attempts[1].Status);
        Assert.True(cooldownStore.IsInCooldown("groq"));
        Assert.False(cooldownStore.IsInCooldown("cerebras"));
    }

    [Fact]
    public async Task CompleteAsync_ProviderInCooldown_IsSkippedOnNextCall()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var cooldownStore = new FreeAiCooldownStore();
        cooldownStore.SetCooldown("groq", TimeSpan.FromMinutes(1));

        var factory = BuildHttpClientFactory(SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory, cooldownStore);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal("cooldown", result.Attempts[0].Status);
    }

    // ─── 401 no primeiro -> usa o segundo, SEM cooldown ─────────────────────────

    [Fact]
    public async Task CompleteAsync_FirstProviderUnauthorized_FallsBackWithoutCooldown()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var cooldownStore = new FreeAiCooldownStore();
        var factory = BuildHttpClientFactory(ErrorResponse(HttpStatusCode.Unauthorized), SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory, cooldownStore);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal("401", result.Attempts[0].Status);
        Assert.False(cooldownStore.IsInCooldown("groq"));
    }

    // ─── Conteúdo vazio -> próximo ───────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_EmptyContent_FallsBackToNextProvider()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(EmptyContentResponse(), SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal("empty_content", result.Attempts[0].Status);
    }

    // ─── Todos falham ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_AllProvidersFail_ReturnsFailureWithAllAttempts()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(ErrorResponse(HttpStatusCode.InternalServerError), ErrorResponse(HttpStatusCode.InternalServerError));
        var service = BuildService(options, BuildSettings(), factory);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Equal(2, result.Attempts.Count);
        Assert.Null(result.Content);
    }

    // ─── Provedor sem chave é pulado ─────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_ProviderWithoutApiKey_IsSkipped()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var settings = BuildSettings(groqKey: null); // groq sem chave
        var factory = BuildHttpClientFactory(SuccessResponse("ola do cerebras"));
        var service = BuildService(options, settings, factory);

        var result = await service.CompleteAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal("no_api_key", result.Attempts[0].Status);
    }

    // ─── Model explícito começa por ele ──────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_ExplicitProviderModel_StartsWithIt()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory);

        var result = await service.CompleteAsync(BuildRequest("cerebras/model-b"));

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
        Assert.Equal("model-b", result.Model);
        Assert.Single(result.Attempts);
    }

    [Fact]
    public async Task CompleteAsync_ModelNameAlone_MatchesChainEntryAndStartsWithIt()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(SuccessResponse("ola do cerebras"));
        var service = BuildService(options, BuildSettings(), factory);

        var result = await service.CompleteAsync(BuildRequest("model-b"));

        Assert.True(result.Success);
        Assert.Equal("cerebras", result.Provider);
    }

    [Fact]
    public async Task CompleteAsync_ModelNameNotInChain_ThrowsArgumentException()
    {
        var options = BuildOptions(("groq", "model-a"));
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(BuildRequest("nao-existe")));
    }

    // ─── BLOQUEANTE custo: provider/model fora da cadeia nunca é aceito ─────────
    // Um "provider/model" arbitrário (ex.: modelo pago da OpenRouter) não pode ser aceito só
    // porque o provider é conhecido — precisa casar com um item que JÁ está na cadeia configurada.
    // Prova negativa: nenhuma chamada HTTP é feita (a validação falha antes do loop de tentativas).
    [Fact]
    public async Task CompleteAsync_ExplicitProviderModelOutsideChain_ThrowsAndMakesNoHttpCall()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var (factory, requests) = BuildTrackedHttpClientFactory();
        var service = BuildService(options, BuildSettings(), factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CompleteAsync(BuildRequest("openrouter/openai/gpt-4o")));

        Assert.Contains("free-ai/v1/models", ex.Message);
        Assert.Empty(requests);
    }

    // ─── Validações ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_NoMessages_ThrowsArgumentException()
    {
        var options = BuildOptions(("groq", "model-a"));
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        var request = new FreeAiChatRequest { Messages = new List<FreeAiChatMessage>() };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(request));
    }

    [Fact]
    public async Task CompleteAsync_InvalidRole_ThrowsArgumentException()
    {
        var options = BuildOptions(("groq", "model-a"));
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        var request = new FreeAiChatRequest
        {
            Messages = new List<FreeAiChatMessage> { new() { Role = "root", Content = "oi" } },
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task CompleteAsync_MaxTokensOutOfRange_ThrowsArgumentException(int maxTokens)
    {
        var options = BuildOptions(("groq", "model-a"));
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        var request = BuildRequest();
        request.MaxTokens = maxTokens;

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(request));
    }

    // ─── Chain padrão quando config vazia ───────────────────────────────────────

    [Fact]
    public void GetChain_EmptyConfig_UsesDefaultChain()
    {
        var options = new FreeAiOptions();
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        Assert.Equal(FreeAiGatewayService.DefaultChain.Count, service.GetChain().Count);
    }

    [Fact]
    public void GetChain_UnsupportedProviderInConfig_IsIgnored()
    {
        var options = new FreeAiOptions
        {
            Chain = new List<FreeAiChainEntry> { new("groq", "model-a"), new("nao-suportado", "x") },
        };
        var service = BuildService(options, BuildSettings(), BuildHttpClientFactory());

        Assert.Single(service.GetChain());
        Assert.Equal("groq", service.GetChain()[0].Provider);
    }

    // ─── Health check ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_AllOk_ReturnsAllOkTrue()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var factory = BuildHttpClientFactory(SuccessResponse(), SuccessResponse());
        var service = BuildService(options, BuildSettings(), factory);

        var health = await service.CheckHealthAsync();

        Assert.Equal(2, health.TotalCount);
        Assert.Equal(2, health.OkCount);
        Assert.All(health.Items, i => Assert.True(i.Ok));
        Assert.False(health.Cached);
    }

    // ─── Cache do /health: 2 chamadas seguidas só martelam o upstream 1x por item ──
    [Fact]
    public async Task CheckHealthAsync_CalledTwiceInARow_OnlyHitsUpstreamOncePerItem()
    {
        var options = BuildOptions(("groq", "model-a"), ("cerebras", "model-b"));
        var (factory, requests) = BuildTrackedHttpClientFactory(SuccessResponse(), SuccessResponse());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = BuildService(options, BuildSettings(), factory, memoryCache: cache);

        var first = await service.CheckHealthAsync();
        var second = await service.CheckHealthAsync();

        Assert.False(first.Cached);
        Assert.True(second.Cached);
        Assert.Equal(2, requests.Count); // um por item da cadeia — a 2ª chamada não gerou novas
    }

    // ─── Chave inválida no banco: tenta a chave do servidor ─────────────────────

    [Fact]
    public async Task ChaveRecusada401_TentaAChaveDoServidor_EAProximaChamadaJaComecaPorEla()
    {
        // 27/10 em produção: Groq/OpenRouter davam 401 com a credencial do banco e a chave do servidor era válida.
        var options = BuildOptions(("groq", "model-a"));
        options.ServerKeys["groq"] = new List<string> { "chave-do-servidor" };
        var (factory, requests) = BuildTrackedHttpClientFactory(
            ErrorResponse(HttpStatusCode.Unauthorized), SuccessResponse(), SuccessResponse());
        var service = BuildService(options, BuildSettings(groqKey: "chave-velha"), factory, new FreeAiCooldownStore());

        var first = await service.CompleteAsync(BuildRequest());
        var second = await service.CompleteAsync(BuildRequest());

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(
            new[] { "Bearer chave-velha", "Bearer chave-do-servidor", "Bearer chave-do-servidor" },
            requests.Select(r => r.Headers.Authorization!.ToString()).ToArray());
    }

    [Fact]
    public async Task RateLimit429_NaoTrocaDeChave()
    {
        // 429 é cota do provedor, não credencial: trocar de chave só gastaria outra cota.
        var options = BuildOptions(("groq", "model-a"));
        options.ServerKeys["groq"] = new List<string> { "chave-do-servidor" };
        var (factory, requests) = BuildTrackedHttpClientFactory(ErrorResponse(HttpStatusCode.TooManyRequests));
        var service = BuildService(options, BuildSettings(groqKey: "chave-velha"), factory, new FreeAiCooldownStore());

        var result = await service.CompleteAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Single(requests);
    }

    [Fact]
    public async Task CheckHealthAsync_NuncaUsaORepositorioEmParalelo()
    {
        // Os repositórios compartilham o DbContext do request: duas consultas ao mesmo tempo quebram
        // ("A second operation was started on this context"). O health é paralelo só no HTTP.
        var options = BuildOptions(("groq", "a"), ("cerebras", "b"), ("gemini", "c"), ("openrouter", "d:free"));
        var (factory, _) = BuildTrackedHttpClientFactory(SuccessResponse(), SuccessResponse(), SuccessResponse(), SuccessResponse());
        var inFlight = 0; var maxInFlight = 0;
        var providerRepo = new Mock<IAiProviderRepository>();
        providerRepo.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                var now = Interlocked.Increment(ref inFlight);
                lock (providerRepo) maxInFlight = Math.Max(maxInFlight, now);
                await Task.Delay(30);
                Interlocked.Decrement(ref inFlight);
                return (AiProvider?)null;
            });
        var service = BuildService(options, BuildSettings(), factory, memoryCache: new MemoryCache(new MemoryCacheOptions()), providerRepoOverride: providerRepo);

        await service.CheckHealthAsync();

        Assert.Equal(1, maxInFlight);
    }

    [Fact]
    public async Task CheckHealthAsync_ForceRefreshWithinMinInterval_StillServesCache()
    {
        var options = BuildOptions(("groq", "model-a"));
        var (factory, requests) = BuildTrackedHttpClientFactory(SuccessResponse());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = BuildService(options, BuildSettings(), factory, memoryCache: cache);

        await service.CheckHealthAsync();
        var second = await service.CheckHealthAsync(forceRefresh: true);

        Assert.True(second.Cached);
        Assert.Single(requests); // forceRefresh ignorado: último check tem < 15s
    }
}
