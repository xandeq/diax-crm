using Diax.Application.AI.QuotaManagement;

namespace Diax.Application.AI.VideoGeneration.Dtos;

public record VideoGenerationResponseDto(
    string ProviderUsed,
    string ModelUsed,
    string RequestId,
    int DurationMs,
    string VideoUrl,
    string? ThumbnailUrl,
    QuotaStatusDto? QuotaStatus = null,
    bool FallbackOccurred = false,
    string? RequestedProvider = null,
    List<string>? AttemptedProviders = null
);
