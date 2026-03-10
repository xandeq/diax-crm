using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class HuggingFaceTextClient : BaseLlmClient
{
    public HuggingFaceTextClient(HttpClient httpClient, ILogger<HuggingFaceTextClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "huggingface";
}
