using Diax.Application.Common;
using Diax.Domain.Finance.Planner;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Planner;

/// <summary>
/// Serviço responsável por calcular projeções de saldo diário
/// </summary>
public class CashFlowProjectionService : IApplicationService
{
    private readonly ILogger<CashFlowProjectionService> _logger;

    public CashFlowProjectionService(ILogger<CashFlowProjectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calcula a projeção de saldos diários baseado em um saldo inicial e lista de transações
    /// </summary>
    public List<DailyBalanceProjection> ProjectDailyBalances(
        decimal startingBalance,
        List<ProjectedTransaction> transactions,
        Guid simulationId)
    {
        var dailyBalances = new List<DailyBalanceProjection>();
        decimal currentBalance = startingBalance;

        if (!transactions.Any())
        {
            _logger.LogWarning("No transactions provided for cash flow projection");
            return dailyBalances;
        }

        // Agrupar transações por dia
        var transactionsByDay = transactions
            .GroupBy(t => t.Date.Date)
            .OrderBy(g => g.Key)
            .ToList();

        _logger.LogInformation("Projecting cash flow for {DayCount} days with starting balance {Balance:C}",
            transactionsByDay.Count, startingBalance);

        foreach (var dayGroup in transactionsByDay)
        {
            decimal dayIncome = dayGroup
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            decimal dayExpenses = dayGroup
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var closingBalance = currentBalance + dayIncome - dayExpenses;

            var dailyBalance = new DailyBalanceProjection
            {
                SimulationId = simulationId,
                Date = dayGroup.Key,
                OpeningBalance = currentBalance,
                TotalIncome = dayIncome,
                TotalExpenses = dayExpenses,
                ClosingBalance = closingBalance,
                IsNegative = closingBalance < 0,
                HasHighPriorityExpense = dayGroup.Any(t =>
                    t.Type == TransactionType.Expense && t.Priority <= 20)
            };

            dailyBalances.Add(dailyBalance);
            currentBalance = closingBalance;

            if (dailyBalance.IsNegative)
            {
                _logger.LogWarning("Negative balance detected on {Date}: {Balance:C}",
                    dailyBalance.Date, dailyBalance.ClosingBalance);
            }
        }

        _logger.LogInformation("Cash flow projection complete. Final balance: {Balance:C}", currentBalance);
        return dailyBalances;
    }

    /// <summary>
    /// Identifica o primeiro dia com saldo negativo
    /// </summary>
    public DateTime? FindFirstNegativeBalanceDate(List<DailyBalanceProjection> dailyBalances)
    {
        return dailyBalances
            .Where(d => d.IsNegative)
            .OrderBy(d => d.Date)
            .FirstOrDefault()?.Date;
    }

    /// <summary>
    /// Encontra o menor saldo no período
    /// </summary>
    public decimal FindLowestBalance(List<DailyBalanceProjection> dailyBalances)
    {
        return dailyBalances.Any()
            ? dailyBalances.Min(d => d.ClosingBalance)
            : 0;
    }

    /// <summary>
    /// Verifica se há risco de saldo negativo
    /// </summary>
    public bool HasNegativeBalanceRisk(List<DailyBalanceProjection> dailyBalances)
    {
        return dailyBalances.Any(d => d.IsNegative);
    }
}
