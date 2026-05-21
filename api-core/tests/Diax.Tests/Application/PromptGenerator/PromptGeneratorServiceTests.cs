using System.Net;
using System.Text;
using System.Text.Json;
using Diax.Application.AI;
using Diax.Application.AI.Services;
using Diax.Application.Common;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Diax.Domain.PromptGenerator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Diax.Tests.Application.PromptGenerator;

/// <summary>
/// Tests for PromptGeneratorService provider routing and isolation.
/// Regression suite for: provider=anthropic being silently routed to Gemini.
/// </summary>
public class PromptGeneratorServiceTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static PromptGeneratorSettings BuildSettings(
        string? anthropicKey = "sk-ant-test",
        string? openAiKey = null,
        string? geminiKey = null,
        string? deepSeekKey = null,
        string? openRouterKey = null)
    {
        return new PromptGeneratorSettings
        {
            TimeoutSeconds = 5,
            Anthropic = new ProviderConfig { ApiKey = anthropicKey, BaseUrl = "https://api.anthropic.com/v1", Model = "claude-haiku-4-5" },
            OpenAI = new ProviderConfig { ApiKey = openAiKey, BaseUrl = "https://api.openai.com/v1", Model = "gpt-4o-mini" },
            Gemini = new ProviderConfig { ApiKey = geminiKey, BaseUrl = "https://generativelanguage.googleapis.com/v1beta", Model = "gemini-2.0-flash" },
            DeepSeek = new ProviderConfig { ApiKey = deepSeekKey, BaseUrl = "https://api.deepseek.com", Model = "deepseek-chat" },
            OpenRouter = new ProviderConfig { ApiKey = openRouterKey, BaseUrl = "https://openrouter.ai/api/v1", Model = "deepseek/deepseek-chat-v3-0324" },
        };
    }

    private static Mock<IAiModelValidator> BuildValidator(
        string providerKey,
        string modelKey,
        bool isValid = true)
    {
        var mock = new Mock<IAiModelValidator>();
        mock.Setup(v => v.IsValidModelAsync(providerKey, modelKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isValid);
        mock.Setup(v => v.GetActiveModelKeysAsync(providerKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isValid ? new[] { modelKey } : Array.Empty<string>());
        return mock;
    }

    private static IHttpClientFactory BuildHttpClientFactoryMock(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static PromptGeneratorService BuildService(
        PromptGeneratorSettings settings,
        IAiModelValidator validator,
        IHttpClientFactory httpClientFactory)
    {
        return new PromptGeneratorService(
            NullLogger<PromptGeneratorService>.Instance,
            settings,
            new Mock<IUserPromptRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            validator,
            httpClientFactory,
            new Mock<IAiUsageTrackingService>().Object,
            new Mock<IAiProviderRepository>().Object,
            new Mock<IAiModelRepository>().Object);
    }

    private static HttpResponseMessage AnthropicSuccessResponse(string text = "Generated prompt") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    content = new[] { new { type = "text", text } },
                    stop_reason = "end_turn"
                }),
                Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage OpenAiSuccessResponse(string text = "Generated prompt") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { content = text } } }
                }),
                Encoding.UTF8, "application/json")
        };

    // ─── Bug Regression: Anthropic never routes to Gemini ───────────────────────

    [Fact]
    public async Task GenerateAsync_Anthropic_CallsAnthropicEndpoint_NotGemini()
    {
        // Arrange
        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(AnthropicSuccessResponse());

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = BuildValidator("anthropic", "claude-haiku-4-5");
        var settings = BuildSettings(anthropicKey: "sk-ant-test");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act
        await service.GenerateAsync("quero comprar um canguru portador", "anthropic", "professional", "claude-haiku-4-5");

        // Assert: exactly one request, and it goes to the Anthropic endpoint
        Assert.Single(capturedRequests);
        var req = capturedRequests[0];
        Assert.Contains("api.anthropic.com", req.RequestUri!.Host);
        Assert.Contains("/v1/messages", req.RequestUri.PathAndQuery);
        Assert.False(req.Headers.Contains("Authorization"), "Anthropic must NOT use Authorization header");
        Assert.True(req.Headers.Contains("x-api-key"), "Anthropic MUST use x-api-key header");
        Assert.True(req.Headers.Contains("anthropic-version"), "Anthropic MUST send anthropic-version header");
    }

    [Fact]
    public async Task GenerateAsync_Anthropic_NeverCallsGeminiEndpoint()
    {
        // Arrange
        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(AnthropicSuccessResponse());

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = BuildValidator("anthropic", "claude-haiku-4-5");
        // Gemini also has a key but should NOT be called
        var settings = BuildSettings(anthropicKey: "sk-ant-test", geminiKey: "gemini-key");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act
        await service.GenerateAsync("buy a baby kangaroo carrier", "anthropic", "professional", "claude-haiku-4-5");

        // Assert: no request went to Gemini
        var geminiRequests = capturedRequests.Where(r => r.RequestUri!.Host.Contains("googleapis.com")).ToList();
        Assert.Empty(geminiRequests);
    }

    [Fact]
    public async Task GenerateAsync_Anthropic_ReturnsExpectedText()
    {
        // Arrange
        const string expected = "# Prompt Profissional\n\nContexto: compra de canguru portador";

        var validator = BuildValidator("anthropic", "claude-haiku-4-5");
        var httpFactory = BuildHttpClientFactoryMock(AnthropicSuccessResponse(expected));
        var settings = BuildSettings(anthropicKey: "sk-ant-test");
        var service = BuildService(settings, validator.Object, httpFactory);

        // Act
        var result = await service.GenerateAsync("quero comprar um canguru portador", "anthropic", "professional", "claude-haiku-4-5");

        // Assert
        Assert.Equal(expected, result);
    }

    // ─── Provider Isolation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_GeminiSelected_CallsGeminiEndpoint_NotAnthropic()
    {
        // Arrange
        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        candidates = new[] { new { content = new { parts = new[] { new { text = "ok" } } } } }
                    }),
                    Encoding.UTF8, "application/json")
            });

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = new Mock<IAiModelValidator>();
        validator.Setup(v => v.IsValidModelAsync("gemini", "gemini-2.0-flash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        validator.Setup(v => v.GetActiveModelKeysAsync("gemini", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "gemini-2.0-flash" });

        var settings = BuildSettings(anthropicKey: "sk-ant", geminiKey: "gemini-key");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act
        await service.GenerateAsync("test prompt", "gemini", "professional", "gemini-2.0-flash");

        // Assert: only Gemini was called
        var anthropicRequests = capturedRequests.Where(r => r.RequestUri!.Host.Contains("anthropic.com")).ToList();
        Assert.Empty(anthropicRequests);

        var geminiRequests = capturedRequests.Where(r => r.RequestUri!.Host.Contains("googleapis.com")).ToList();
        Assert.Single(geminiRequests);
    }

    // ─── Provider Default Mapping ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_AnthropicAlias_Claude_RoutesToAnthropic()
    {
        // Arrange
        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(AnthropicSuccessResponse());

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = BuildValidator("anthropic", "claude-haiku-4-5");
        var settings = BuildSettings(anthropicKey: "sk-ant-test");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // "claude" should be an alias for "anthropic"
        await service.GenerateAsync("test", "claude", "professional", "claude-haiku-4-5");

        var req = Assert.Single(capturedRequests);
        Assert.Contains("api.anthropic.com", req.RequestUri!.Host);
    }

    // ─── Anthropic API Key missing ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_AnthropicNoKey_ThrowsWhenNoFallback()
    {
        // Arrange: no provider has an API key except Anthropic being empty
        var settings = BuildSettings(anthropicKey: null, openAiKey: null, geminiKey: null, deepSeekKey: null, openRouterKey: null);

        var validator = new Mock<IAiModelValidator>();
        // Make all providers return empty models (so they're skipped)
        validator.Setup(v => v.GetActiveModelKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        validator.Setup(v => v.IsValidModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var httpFactory = BuildHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
        var service = BuildService(settings, validator.Object, httpFactory);

        // Act & Assert: should throw because no provider has a key
        await Assert.ThrowsAsync<PromptGeneratorException>(() =>
            service.GenerateAsync("test", "anthropic", "professional", "claude-haiku-4-5"));
    }

    // ─── Fallback: Gemini config error doesn't stop chain ───────────────────────

    [Fact]
    public async Task GenerateAsync_GeminiFailsOnFallback_DoesNotBubbleArgumentException()
    {
        // Arrange: User selects DeepSeek (which has no key), so system falls to Anthropic which works
        // This tests that Gemini's ArgumentException on fallback doesn't stop the chain

        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
            .ReturnsAsync(AnthropicSuccessResponse("fallback worked"));

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = new Mock<IAiModelValidator>();
        // Anthropic is valid
        validator.Setup(v => v.IsValidModelAsync("anthropic", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        validator.Setup(v => v.GetActiveModelKeysAsync("anthropic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "claude-haiku-4-5" });
        // All others return empty/invalid so they're skipped
        validator.Setup(v => v.GetActiveModelKeysAsync(It.IsNotIn("anthropic"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        validator.Setup(v => v.IsValidModelAsync(It.IsNotIn("anthropic"), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // deepseek has no key, openrouter has no key, anthropic has a key, gemini has key but invalid model
        var settings = BuildSettings(
            anthropicKey: "sk-ant-test",
            deepSeekKey: null,
            openRouterKey: null,
            geminiKey: "gemini-key",
            openAiKey: null);

        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act: select deepseek (no key → skip) → fallback to anthropic
        var result = await service.GenerateAsync("test", "deepseek", "professional", "deepseek-chat");

        // Assert: succeeded via anthropic fallback, not stopped by Gemini error
        Assert.Equal("fallback worked", result);
        Assert.True(capturedRequests.Any(r => r.RequestUri!.Host.Contains("anthropic.com")));
    }

    // ─── Anthropic Request Format ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_Anthropic_SendsCorrectJsonFormat()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(AnthropicSuccessResponse());

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = BuildValidator("anthropic", "claude-haiku-4-5");
        var settings = BuildSettings(anthropicKey: "sk-ant-test");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act
        await service.GenerateAsync("test prompt", "anthropic", "professional", "claude-haiku-4-5");

        // Assert body format
        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("model", out var model), "Must send 'model'");
        Assert.Equal("claude-haiku-4-5", model.GetString());

        Assert.True(root.TryGetProperty("max_tokens", out var maxTokens), "Must send 'max_tokens'");
        Assert.True(maxTokens.GetInt32() > 0, "max_tokens must be > 0");

        Assert.True(root.TryGetProperty("system", out _), "Must send 'system' prompt");
        Assert.True(root.TryGetProperty("messages", out var messages), "Must send 'messages' array");
        Assert.Equal(1, messages.GetArrayLength());

        var firstMessage = messages[0];
        Assert.Equal("user", firstMessage.GetProperty("role").GetString());
        Assert.Equal("test prompt", firstMessage.GetProperty("content").GetString());

        // Must NOT have 'choices' format (that's OpenAI)
        Assert.False(root.TryGetProperty("choices", out _), "Must NOT use OpenAI 'choices' format");
    }

    // ─── Cross-provider model contamination guard ────────────────────────────────

    [Fact]
    public async Task GenerateAsync_Anthropic_WithGeminiModel_FallsBackToConfigDefault()
    {
        // When a non-Anthropic model (e.g. "gemini-2.5-flash") is requested for the Anthropic provider,
        // BuildSettings rejects it via IsValidModelAsync and falls back to the config default.
        // The request then proceeds with the config default model, not the Gemini model.

        string? capturedModelInBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                var body = await req.Content!.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                capturedModelInBody = doc.RootElement.GetProperty("model").GetString();
            })
            .ReturnsAsync(AnthropicSuccessResponse());

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var validator = new Mock<IAiModelValidator>();
        // gemini-2.5-flash is invalid for anthropic
        validator.Setup(v => v.IsValidModelAsync("anthropic", "gemini-2.5-flash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        // config default (claude-3-haiku) is valid
        validator.Setup(v => v.IsValidModelAsync("anthropic", "claude-haiku-4-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        validator.Setup(v => v.GetActiveModelKeysAsync("anthropic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "claude-haiku-4-5", "claude-sonnet-4-5" });

        var settings = BuildSettings(anthropicKey: "sk-ant-test");
        var service = BuildService(settings, validator.Object, httpFactory.Object);

        // Act: gemini model requested but it's invalid for Anthropic, falls to config default
        await service.GenerateAsync("test", "anthropic", "professional", "gemini-2.5-flash");

        // Assert: request used the Anthropic config default model, NOT the gemini model
        Assert.NotNull(capturedModelInBody);
        Assert.DoesNotContain("gemini", capturedModelInBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("claude-haiku-4-5", capturedModelInBody);
    }
}
