using Diax.Domain.Common;

namespace Diax.Domain.AI;

/// <summary>
/// Logs AI usage for audit, cost tracking, and analytics
/// </summary>
public class AiUsageLog : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid ModelId { get; private set; }
    public string FeatureType { get; private set; } // "PromptGeneration" | "Humanization" | "VideoGeneration"
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string RequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Video-specific fields
    public int? QuotaCreditsUsed { get; private set; }
    public int? GenerationDurationSeconds { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public int? VideoWidth { get; private set; }
    public int? VideoHeight { get; private set; }
    public string? AspectRatio { get; private set; }

    // Error observability — populated on failure
    public string? ErrorCategory { get; private set; }  // AiErrorCategory constant
    public int? HttpStatusCode { get; private set; }    // HTTP status from provider response (when available)

    // Navigation properties
    public AiProvider Provider { get; private set; } = null!;
    public AiModel Model { get; private set; } = null!;

    private AiUsageLog() { } // EF Core

    public AiUsageLog(
        Guid userId,
        Guid providerId,
        Guid modelId,
        string featureType,
        TimeSpan duration,
        bool success,
        string requestId,
        int? inputTokens = null,
        int? outputTokens = null,
        decimal? estimatedCost = null,
        string? errorMessage = null,
        int? quotaCreditsUsed = null,
        int? generationDurationSeconds = null,
        string? thumbnailUrl = null,
        int? videoWidth = null,
        int? videoHeight = null,
        string? aspectRatio = null,
        string? errorCategory = null,
        int? httpStatusCode = null)
    {
        UserId = userId;
        ProviderId = providerId;
        ModelId = modelId;
        FeatureType = featureType;
        Duration = duration;
        Success = success;
        RequestId = requestId;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        EstimatedCost = estimatedCost;
        ErrorMessage = errorMessage;
        CreatedAt = DateTime.UtcNow;
        QuotaCreditsUsed = quotaCreditsUsed;
        GenerationDurationSeconds = generationDurationSeconds;
        ThumbnailUrl = thumbnailUrl;
        VideoWidth = videoWidth;
        VideoHeight = videoHeight;
        AspectRatio = aspectRatio;
        ErrorCategory = errorCategory;
        HttpStatusCode = httpStatusCode;
    }

    public void UpdateTokensAndCost(int? inputTokens, int? outputTokens, decimal? estimatedCost)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        EstimatedCost = estimatedCost;
    }
}
