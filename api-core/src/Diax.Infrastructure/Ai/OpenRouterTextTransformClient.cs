using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class OpenRouterTextTransformClient : BaseLlmClient
{
    public OpenRouterTextTransformClient(HttpClient httpClient, ILogger<OpenRouterTextTransformClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "openrouter";
}
