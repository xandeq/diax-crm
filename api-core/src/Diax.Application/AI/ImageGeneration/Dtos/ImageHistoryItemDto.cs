namespace Diax.Application.AI.ImageGeneration.Dtos;

public record ImageHistoryItemDto(
    Guid Id,
    string ImageUrl,
    string Prompt,
    string? ProviderName,
    string? ModelName,
    int Width,
    int Height,
    decimal? EstimatedCostUsd,
    DateTime CreatedAt
);
