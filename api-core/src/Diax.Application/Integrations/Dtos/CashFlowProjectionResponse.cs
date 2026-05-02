namespace Diax.Application.Integrations.Dtos;

public record CashFlowProjectionResponse(
    decimal CurrentBalance,
    decimal AvailableToInvest,
    DateTime FromDate,
    DateTime ToDate,
    NextBigOutflow? NextBigOutflow,
    List<DailyBalanceItem> DailyProjection
);

public record NextBigOutflow(DateTime Date, decimal Amount, string Description);

public record DailyBalanceItem(
    DateTime Date,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal ClosingBalance,
    bool IsNegative,
    bool HasHighPriorityExpense
);
