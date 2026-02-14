using Diax.Application.AI.Dtos;

namespace Diax.Application.AI;

public interface IAnthropicClient
{
    Task<AnthropicModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}
