namespace Diax.Application.AI.Dtos;

/// <param name="AvailabilityStatus">
/// Runtime health status derived from failure tracking. Values: Available, Degraded, Unavailable,
/// NoCredits, RateLimited, ConfigError. This is a READ-ONLY computed field — it never triggers
/// auto-disabling. IsEnabled is a separate flag controlled only by admins.
/// </param>
/// <param name="ConsecutiveFailureCount">How many consecutive failures this model has had since the last success.</param>
/// <param name="LastFailureAt">UTC timestamp of the most recent failure, if any.</param>
/// <param name="LastSuccessAt">UTC timestamp of the most recent success, if any.</param>
/// <param name="LastFailureCategory">AiErrorCategory constant for the most recent failure.</param>
public record AiModelDto(
    Guid Id,
    string ModelKey,
    string DisplayName,
    bool IsEnabled,
    bool IsDiscovered,
    decimal? InputCostHint,
    decimal? OutputCostHint,
    int? MaxTokensHint,
    bool SupportsImage,
    bool SupportsText,
    bool SupportsVideo,
    string AvailabilityStatus,
    int ConsecutiveFailureCount,
    DateTime? LastFailureAt,
    DateTime? LastSuccessAt,
    string? LastFailureCategory
);
