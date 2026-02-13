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
    public string FeatureType { get; private set; } // "PromptGeneration" | "Humanization"
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string RequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
        string? errorMessage = null)
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
    }

    public void UpdateTokensAndCost(int? inputTokens, int? outputTokens, decimal? estimatedCost)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        EstimatedCost = estimatedCost;
    }
}
