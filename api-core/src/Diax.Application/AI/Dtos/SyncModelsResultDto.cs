namespace Diax.Application.AI.Dtos;

public record SyncModelsResultDto(
    int DiscoveredCount,
    int NewModels,
    int ExistingModelsUpdated,
    List<string> Errors
);
