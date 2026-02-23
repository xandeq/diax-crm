namespace Diax.Application.AI.ImageGeneration.Dtos;

public record GeneratedImageDto(
    Guid Id,
    string ImageUrl,
    string? RevisedPrompt,
    string? Seed,
    int Width,
    int Height
);

public record ImageGenerationResponseDto(
    Guid ProjectId,
    string ProviderUsed,
    string ModelUsed,
    string RequestId,
    int DurationMs,
    List<GeneratedImageDto> Images
);
