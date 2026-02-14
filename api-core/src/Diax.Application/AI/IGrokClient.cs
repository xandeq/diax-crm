using Diax.Application.AI.Dtos;

namespace Diax.Application.AI;

public interface IGrokClient
{
    Task<GrokModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}
