using Diax.Domain.Finance.Planner;

namespace Diax.Tests.Domain.Finance;

public class FinancialGoalTests
{
    private static FinancialGoal NewGoal(decimal target, decimal current = 0m) => new()
    {
        UserId = Guid.NewGuid(),
        Name = "Reserva de Emergência",
        TargetAmount = target,
        CurrentAmount = current,
        Category = GoalCategory.Emergency,
        Priority = 5,
        IsActive = true,
        AutoAllocateSurplus = false,
    };

    // ── AddContribution ──────────────────────────────────────────────

    [Fact]
    public void AddContribution_ValidAmount_IncreasesCurrentAmount()
    {
        var goal = NewGoal(1000m, 200m);
        goal.AddContribution(300m);
        Assert.Equal(500m, goal.CurrentAmount);
    }

    [Fact]
    public void AddContribution_Zero_ThrowsArgumentException()
    {
        var goal = NewGoal(1000m);
        Assert.Throws<ArgumentException>(() => goal.AddContribution(0m));
    }

    [Fact]
    public void AddContribution_NegativeAmount_ThrowsArgumentException()
    {
        var goal = NewGoal(1000m, 500m);
        Assert.Throws<ArgumentException>(() => goal.AddContribution(-50m));
    }

    [Fact]
    public void AddContribution_ExceedsTarget_ClampsToTarget()
    {
        var goal = NewGoal(1000m, 800m);
        goal.AddContribution(500m);
        Assert.Equal(1000m, goal.CurrentAmount);
    }

    [Fact]
    public void AddContribution_ExactlyRemainingAmount_ReachesTarget()
    {
        var goal = NewGoal(1000m, 750m);
        goal.AddContribution(250m);
        Assert.Equal(1000m, goal.CurrentAmount);
        Assert.True(goal.IsCompleted());
    }

    // ── GetProgress ──────────────────────────────────────────────────

    [Fact]
    public void GetProgress_ZeroTarget_ReturnsZero()
    {
        var goal = NewGoal(0m);
        Assert.Equal(0m, goal.GetProgress());
    }

    [Fact]
    public void GetProgress_HalfFunded_Returns50()
    {
        var goal = NewGoal(1000m, 500m);
        Assert.Equal(50m, goal.GetProgress());
    }

    [Fact]
    public void GetProgress_FullyFunded_Returns100()
    {
        var goal = NewGoal(1000m, 1000m);
        Assert.Equal(100m, goal.GetProgress());
    }

    [Fact]
    public void GetProgress_RoundsToTwoDecimals()
    {
        var goal = NewGoal(300m, 100m);
        Assert.Equal(33.33m, goal.GetProgress());
    }

    // ── GetRemainingAmount ───────────────────────────────────────────

    [Fact]
    public void GetRemainingAmount_PartiallyFunded_ReturnsPositiveRemainder()
    {
        var goal = NewGoal(1000m, 400m);
        Assert.Equal(600m, goal.GetRemainingAmount());
    }

    [Fact]
    public void GetRemainingAmount_Completed_ReturnsZero()
    {
        var goal = NewGoal(1000m, 1000m);
        Assert.Equal(0m, goal.GetRemainingAmount());
    }

    [Fact]
    public void GetRemainingAmount_Empty_ReturnsFullTarget()
    {
        var goal = NewGoal(500m);
        Assert.Equal(500m, goal.GetRemainingAmount());
    }

    // ── IsCompleted ──────────────────────────────────────────────────

    [Fact]
    public void IsCompleted_CurrentLessThanTarget_ReturnsFalse()
    {
        var goal = NewGoal(1000m, 999m);
        Assert.False(goal.IsCompleted());
    }

    [Fact]
    public void IsCompleted_CurrentEqualsTarget_ReturnsTrue()
    {
        var goal = NewGoal(1000m, 1000m);
        Assert.True(goal.IsCompleted());
    }

    [Fact]
    public void IsCompleted_ZeroTarget_TreatedAsComplete()
    {
        var goal = NewGoal(0m, 0m);
        Assert.True(goal.IsCompleted());
    }
}
