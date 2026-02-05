namespace Diax.Application.AI.Dtos;

public record AiModelDto(
    Guid Id,
    string ModelKey,
    string DisplayName,
    bool IsEnabled,
    bool IsDiscovered,
    decimal? InputCostHint,
    decimal? OutputCostHint,
    int? MaxTokensHint
);
