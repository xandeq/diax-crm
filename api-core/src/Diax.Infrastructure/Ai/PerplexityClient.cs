using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class PerplexityClient : BaseLlmClient
{
    public PerplexityClient(HttpClient httpClient, ILogger<PerplexityClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "perplexity";
}
