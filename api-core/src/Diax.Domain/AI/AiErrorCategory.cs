namespace Diax.Domain.AI;

/// <summary>
/// Categorizes AI provider/model failures for observability and diagnostic purposes.
/// Used to distinguish transient errors (temporary) from structural errors (permanent).
/// </summary>
public static class AiErrorCategory
{
    /// <summary>Unknown or unmapped error. Should be investigated.</summary>
    public const string Unknown = "Unknown";

    /// <summary>API credit balance exhausted. Transient — resolves when credits are topped up.</summary>
    public const string QuotaExhausted = "QuotaExhausted";

    /// <summary>Daily/monthly generation limit reached. Transient — resolves on quota reset.</summary>
    public const string RateLimit = "RateLimit";

    /// <summary>Authentication or API key failure. May be structural if key is invalid/revoked.</summary>
    public const string AuthFailed = "AuthFailed";

    /// <summary>Model key not found or deprecated on the provider side. Structural.</summary>
    public const string ModelNotFound = "ModelNotFound";

    /// <summary>Invalid request parameters. May be structural (wrong model for feature type).</summary>
    public const string InvalidRequest = "InvalidRequest";

    /// <summary>Provider is temporarily unavailable or returned server errors (5xx). Transient.</summary>
    public const string ProviderUnavailable = "ProviderUnavailable";

    /// <summary>Request timed out. Transient — usually a cold start or overloaded provider.</summary>
    public const string Timeout = "Timeout";

    /// <summary>API key or credentials not configured in the system. Structural.</summary>
    public const string ConfigurationMissing = "ConfigurationMissing";

    /// <summary>Model does not support the requested feature (e.g., image gen via a text-only model). Structural.</summary>
    public const string CapabilityMismatch = "CapabilityMismatch";

    /// <summary>
    /// Returns true if the error category is transient (recovers without admin intervention).
    /// </summary>
    public static bool IsTransient(string? category) => category switch
    {
        QuotaExhausted => true,
        RateLimit => true,
        ProviderUnavailable => true,
        Timeout => true,
        _ => false
    };

    /// <summary>
    /// Returns true if the error category is structural (requires configuration or code fix).
    /// </summary>
    public static bool IsStructural(string? category) => category switch
    {
        AuthFailed => true,
        ModelNotFound => true,
        InvalidRequest => true,
        ConfigurationMissing => true,
        CapabilityMismatch => true,
        _ => false
    };

    /// <summary>
    /// Derives an availability status string from the failure tracking state.
    /// Used in the catalog DTO for the frontend to display appropriate badges.
    /// </summary>
    public static string ComputeAvailabilityStatus(
        int consecutiveFailureCount,
        string? lastFailureCategory,
        DateTime? lastSuccessAt,
        DateTime? lastFailureAt)
    {
        if (consecutiveFailureCount == 0)
            return AvailabilityStatus.Available;

        // Structural errors surface as specific statuses regardless of count
        if (lastFailureCategory == AuthFailed || lastFailureCategory == ConfigurationMissing)
            return AvailabilityStatus.ConfigError;

        if (lastFailureCategory == ModelNotFound || lastFailureCategory == CapabilityMismatch)
            return AvailabilityStatus.Unavailable;

        if (lastFailureCategory == QuotaExhausted)
            return AvailabilityStatus.NoCredits;

        if (lastFailureCategory == RateLimit)
            return AvailabilityStatus.RateLimited;

        // Transient errors: escalate based on consecutive count
        if (consecutiveFailureCount >= 3)
            return AvailabilityStatus.Unavailable;

        if (consecutiveFailureCount >= 1)
            return AvailabilityStatus.Degraded;

        return AvailabilityStatus.Available;
    }
}

/// <summary>
/// Well-known availability status values returned in the catalog DTO.
/// Consumed by the frontend to show appropriate badges on model cards.
/// </summary>
public static class AvailabilityStatus
{
    public const string Available = "Available";
    public const string Degraded = "Degraded";
    public const string Unavailable = "Unavailable";
    public const string NoCredits = "NoCredits";
    public const string RateLimited = "RateLimited";
    public const string ConfigError = "ConfigError";
}
