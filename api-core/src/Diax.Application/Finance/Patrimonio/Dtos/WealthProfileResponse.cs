namespace Diax.Application.Finance.Patrimonio.Dtos;

public record WealthProfileResponse(
    Guid Id,
    string RiskProfile,
    decimal? GoalAmount,
    int? GoalYears,
    IReadOnlyDictionary<string, decimal> TargetAllocation,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
