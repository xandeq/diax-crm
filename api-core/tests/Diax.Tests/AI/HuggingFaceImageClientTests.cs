using System.Net;
using System.Text;
using Diax.Infrastructure.Ai;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace Diax.Tests.AI;

public class HuggingFaceImageClientTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsGatedModelMessage_WhenProviderReturnsForbiddenForGatedModel()
    {
        var client = CreateClient(HttpStatusCode.Forbidden, """{"error":"Access to model stabilityai/stable-diffusion-3-medium-diffusers is restricted. Please accept the license terms."}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GenerateAsync("prompt", CreateOptions(), ct: CancellationToken.None));

        Assert.Contains("modelo gated", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aceite os termos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsInvalidTokenMessage_WhenProviderReturnsUnauthorized()
    {
        var client = CreateClient(HttpStatusCode.Unauthorized, """{"error":"Invalid credentials in Authorization header"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GenerateAsync("prompt", CreateOptions(), ct: CancellationToken.None));

        Assert.Contains("inválido ou expirado", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Inference API", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsScopeMessage_WhenProviderReturnsForbiddenForScopes()
    {
        var client = CreateClient(HttpStatusCode.Forbidden, """{"error":"Insufficient permissions to call this inference endpoint"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GenerateAsync("prompt", CreateOptions(), ct: CancellationToken.None));

        Assert.Contains("não possui permissão suficiente", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scopes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static HuggingFaceImageClient CreateClient(HttpStatusCode statusCode, string body)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }));

        return new HuggingFaceImageClient(httpClient, NullLogger<HuggingFaceImageClient>.Instance);
    }

    private static ImageGenerationOptions CreateOptions()
        => new(
            ApiKey: "hf_test_token",
            BaseUrl: string.Empty,
            Model: "stabilityai/stable-diffusion-3-medium-diffusers",
            Width: 1024,
            Height: 1024,
            NumberOfImages: 1,
            NegativePrompt: null,
            Seed: null,
            Style: null,
            Quality: null);

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
