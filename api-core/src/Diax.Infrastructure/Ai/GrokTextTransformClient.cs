using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class GrokTextTransformClient : BaseLlmClient
{
    public override string ProviderName => "grok";

    public GrokTextTransformClient(
        HttpClient httpClient,
        ILogger<GrokTextTransformClient> logger)
        : base(httpClient, logger)
    {
    }
}
