using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class GroqClient : BaseLlmClient
{
    public GroqClient(HttpClient httpClient, ILogger<GroqClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "groq";
}
