namespace Diax.Domain.Agents;

/// <summary>
/// Represents a write action requested by an AI agent that requires explicit user confirmation
/// before it is executed. This is the source of truth for the confirmation flow (ORCH-04).
///
/// Invariants:
/// - IsPending == true  → action has not been executed, cancelled, or expired.
/// - IsPending == false → action is done (ExecutedAt set), cancelled (CancelledAt set), or expired.
/// - No data is written until the action is confirmed via /confirm endpoint.
/// </summary>
public class AgentPendingAction
{
    // Private ctor for EF Core
    private AgentPendingAction() { }

    public AgentPendingAction(
        Guid userId,
        Guid conversationId,
        AgentType agentType,
        string toolName,
        string toolUseId,
        string actionLabel,
        string payload,
        int ttlMinutes = 15)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("ToolName is required.", nameof(toolName));
        if (string.IsNullOrWhiteSpace(toolUseId))
            throw new ArgumentException("ToolUseId is required.", nameof(toolUseId));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        Id = Guid.NewGuid();
        UserId = userId;
        ConversationId = conversationId;
        AgentType = agentType;
        ToolName = toolName.Trim();
        ToolUseId = toolUseId.Trim();
        ActionLabel = actionLabel ?? string.Empty;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ConversationId { get; private set; }

    /// <summary>The type of agent that created this pending action.</summary>
    public AgentType AgentType { get; private set; }

    /// <summary>The tool name (must match ^[a-zA-Z0-9_-]{1,64}$).</summary>
    public string ToolName { get; private set; } = string.Empty;

    /// <summary>Anthropic's tool_use block id (e.g. toolu_01xxx).</summary>
    public string ToolUseId { get; private set; } = string.Empty;

    /// <summary>Human-readable label shown on the confirmation button in the UI.</summary>
    public string ActionLabel { get; private set; } = string.Empty;

    /// <summary>Serialized JSON payload of the tool input — stored for execution after confirmation.</summary>
    public string Payload { get; private set; } = string.Empty;

    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Set when action is confirmed and executed. Null = not yet executed.</summary>
    public DateTime? ExecutedAt { get; private set; }

    /// <summary>Set when action is cancelled. Null = not cancelled.</summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>True when the TTL window has passed and the action can no longer be confirmed.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// True when the action is waiting for user confirmation.
    /// Becomes false once executed, cancelled, or expired.
    /// </summary>
    public bool IsPending => ExecutedAt is null && CancelledAt is null && !IsExpired;

    /// <summary>Marks this action as executed. Call only after the write has been successfully applied.</summary>
    public void MarkExecuted()
    {
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>Cancels this pending action. The associated write will never be applied.</summary>
    public void Cancel()
    {
        CancelledAt = DateTime.UtcNow;
    }
}
