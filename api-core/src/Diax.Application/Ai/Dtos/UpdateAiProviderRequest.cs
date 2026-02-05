namespace Diax.Application.AI.Dtos;

public record UpdateAiProviderRequest(
    string Name,
    bool SupportsListModels,
    string? BaseUrl,
    bool IsEnabled
);
