using Diax.Domain.Finance.Planner;

namespace Diax.Application.Finance.Planner.Dtos;

public class FinancialGoalResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalCategory Category { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool AutoAllocateSurplus { get; set; }
    public decimal Progress { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateFinancialGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal? CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalCategory Category { get; set; }
    public int Priority { get; set; } = 5;
    public bool AutoAllocateSurplus { get; set; }
}

public class UpdateFinancialGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalCategory Category { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool AutoAllocateSurplus { get; set; }
}

public class AddContributionRequest
{
    public decimal Amount { get; set; }
}
