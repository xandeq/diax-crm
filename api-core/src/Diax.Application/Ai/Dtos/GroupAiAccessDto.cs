namespace Diax.Application.AI.Dtos;

public record GroupAiAccessDto(
    List<Guid> AllowedProviderIds,
    List<Guid> AllowedModelIds
);
