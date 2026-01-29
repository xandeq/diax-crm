namespace Diax.Infrastructure.Ai;

public record AiClientOptions(
    string ApiKey,
    string BaseUrl,
    string Model,
    double Temperature = 0.7,
    int? MaxTokens = null
);

public interface IAiTextTransformClient
{
    string ProviderName { get; }
    Task<string> TransformAsync(string systemPrompt, string userPrompt, AiClientOptions options, CancellationToken ct = default);
}
