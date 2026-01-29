using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

public class ChatGptClient : BaseLlmClient
{
    public ChatGptClient(HttpClient httpClient, ILogger<ChatGptClient> logger)
        : base(httpClient, logger)
    {
    }

    public override string ProviderName => "chatgpt";
}
