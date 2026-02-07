namespace Diax.Application.AI;

/// <summary>
/// Interface for DeepSeek client - model discovery.
/// DeepSeek uses OpenAI-compatible response format.
/// </summary>
public interface IDeepSeekModelClient
{
    Task<OpenAiModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}
