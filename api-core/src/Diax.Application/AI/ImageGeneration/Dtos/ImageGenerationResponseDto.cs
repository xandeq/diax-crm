using Diax.Application.AI.QuotaManagement;

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
    List<GeneratedImageDto> Images,
    bool FallbackOccurred = false,
    string? RequestedProvider = null,
    List<string>? AttemptedProviders = null,
    decimal? EstimatedCostUsd = null,
    QuotaStatusDto? QuotaStatus = null
);
