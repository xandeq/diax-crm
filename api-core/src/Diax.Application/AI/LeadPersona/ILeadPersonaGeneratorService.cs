using Diax.Shared;

namespace Diax.Application.AI.LeadPersona;

public interface ILeadPersonaGeneratorService : IApplicationService
{
    Task<GeneratePersonasResponseDto> GeneratePersonasAsync(
        GeneratePersonasRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
