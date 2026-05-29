using Diax.Domain.Agents;
using Xunit;

namespace Diax.Tests.Application.Agents;

public class AgentPendingActionTests
{
    private static AgentPendingAction CreateAction(int ttlMinutes = 15)
        => new(
            userId: Guid.NewGuid(),
            conversationId: Guid.NewGuid(),
            agentType: AgentType.Commercial,
            toolName: "update_lead_status",
            toolUseId: "toolu_01test",
            actionLabel: "Atualizar status do lead",
            payload: "{\"lead_id\":\"abc\"}",
            ttlMinutes: ttlMinutes);

    [Fact]
    public void NewAction_WithFutureTtl_IsPendingAndNotExpired()
    {
        var action = CreateAction(ttlMinutes: 15);

        Assert.True(action.IsPending);
        Assert.False(action.IsExpired);
        Assert.Null(action.ExecutedAt);
        Assert.Null(action.CancelledAt);
    }

    [Fact]
    public void MarkExecuted_SetsExecutedAt_AndIsPendingBecomesFalse()
    {
        var action = CreateAction();
        Assert.True(action.IsPending);

        action.MarkExecuted();

        Assert.NotNull(action.ExecutedAt);
        Assert.False(action.IsPending);
    }

    [Fact]
    public void Cancel_SetsCancelledAt_AndIsPendingBecomesFalse()
    {
        var action = CreateAction();
        Assert.True(action.IsPending);

        action.Cancel();

        Assert.NotNull(action.CancelledAt);
        Assert.False(action.IsPending);
    }

    [Fact]
    public void ExpiredAction_IsNotPendingAndIsExpired()
    {
        // ExpiresAt in the past via ttlMinutes: -1
        var action = CreateAction(ttlMinutes: -1);

        Assert.True(action.IsExpired);
        Assert.False(action.IsPending);
    }

    [Fact]
    public void Ctor_EmptyToolName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AgentPendingAction(
            Guid.NewGuid(), Guid.NewGuid(), AgentType.Commercial,
            toolName: "", toolUseId: "toolu_01",
            actionLabel: "label", payload: "{}", ttlMinutes: 15));
    }

    [Fact]
    public void Ctor_EmptyToolUseId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AgentPendingAction(
            Guid.NewGuid(), Guid.NewGuid(), AgentType.Commercial,
            toolName: "tool", toolUseId: "",
            actionLabel: "label", payload: "{}", ttlMinutes: 15));
    }

    [Fact]
    public void Ctor_EmptyPayload_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AgentPendingAction(
            Guid.NewGuid(), Guid.NewGuid(), AgentType.Commercial,
            toolName: "tool", toolUseId: "toolu_01",
            actionLabel: "label", payload: "", ttlMinutes: 15));
    }
}
