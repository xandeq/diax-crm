namespace Diax.Application.AI.Dtos;

public record AiProviderDto(
    Guid Id,
    string Key,
    string Name,
    bool IsEnabled,
    bool SupportsListModels,
    string? BaseUrl,
    List<AiModelDto> Models
);
