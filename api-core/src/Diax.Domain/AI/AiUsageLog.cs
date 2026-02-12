using Diax.Domain.Common;

namespace Diax.Domain.AI;

public class AiUsageLog : Entity
{
    public Guid? UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid ModelId { get; private set; }
    public int TokensInput { get; private set; }
    public int TokensOutput { get; private set; }
    public int TotalTokens { get; private set; }
    public decimal CostEstimated { get; private set; }
    public string RequestType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public AiProvider Provider { get; private set; } = null!;
    public AiModel Model { get; private set; } = null!;

    // Private constructor for EF Core
    private AiUsageLog() : base()
    {
        RequestType = string.Empty;
    }

    public AiUsageLog(
        Guid? userId,
        Guid providerId,
        Guid modelId,
        int tokensInput,
        int tokensOutput,
        decimal costEstimated,
        string requestType) : base()
    {
        UserId = userId;
        ProviderId = providerId;
        ModelId = modelId;
        TokensInput = tokensInput;
        TokensOutput = tokensOutput;
        TotalTokens = tokensInput + tokensOutput;
        CostEstimated = costEstimated;
        RequestType = requestType;
        CreatedAt = DateTime.UtcNow;
    }
}
