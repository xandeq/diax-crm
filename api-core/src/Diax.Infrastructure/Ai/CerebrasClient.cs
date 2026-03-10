using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class CerebrasClient : BaseLlmClient
{
    public CerebrasClient(HttpClient httpClient, ILogger<CerebrasClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "cerebras";
}
