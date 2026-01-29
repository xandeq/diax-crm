using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class DeepSeekClient : BaseLlmClient
{
    public DeepSeekClient(HttpClient httpClient, ILogger<DeepSeekClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "deepseek";
}
