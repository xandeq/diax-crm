namespace Diax.Application.AI.VideoGeneration.Dtos;

public record VideoJobDto(
    Guid Id,
    string Status,          // Queued | Processing | Completed | Failed
    string Provider,
    string Model,
    string? ProviderUsed,
    string? ModelUsed,
    string? VideoUrl,
    string? ThumbnailUrl,
    string? ErrorMessage,
    string? ErrorCategory,
    bool FallbackOccurred,
    List<string>? AttemptedProviders,
    int? QueuePosition,     // 0 = próximo; null quando não está mais na fila
    int? DurationMs,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);
