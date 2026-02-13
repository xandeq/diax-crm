namespace Diax.Application.AI.Dtos;

public record CreateAiProviderRequest(
    string Key,
    string Name,
    bool SupportsListModels,
    string? BaseUrl
);
