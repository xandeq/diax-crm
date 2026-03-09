namespace Diax.Application.AI.VideoGeneration.Dtos;

public record VideoGenerationResponseDto(
    string ProviderUsed,
    string ModelUsed,
    string RequestId,
    int DurationMs,
    string VideoUrl,
    string? ThumbnailUrl
);
