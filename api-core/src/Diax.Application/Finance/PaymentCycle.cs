using System.Globalization;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;

namespace Diax.Application.Finance;

/// <summary>
/// Regra da planilha do usuário: cada despesa é paga com o último salário que caiu
/// (data de caixa) antes ou no dia do vencimento real. Despesas anteriores ao primeiro
/// salário do mês ficam sem bloco ("saldo anterior"). O ciclo cobre 1 mês a partir da 1ª data
/// de caixa (ex.: 25/09→24/10) — aproximação da véspera do 1º salário do mês seguinte.
/// </summary>
public static class PaymentCycle
{
    public sealed record Block(
        Guid IncomeId,
        string IncomeName,
        decimal IncomeAmount,
        DateTime CashDate,
        decimal ExpensesTotal,
        int ExpensesCount,
        decimal Balance);

    public sealed record Cycle(DateTime Start, DateTime End, string Label);

    public sealed record Result(
        IReadOnlyDictionary<Guid, TransactionResponse> PaidWith,
        IReadOnlyList<Block> Blocks,
        decimal UnassignedTotal,
        int UnassignedCount,
        Cycle? Cycle);

    public static DateTime CashDate(TransactionResponse income) => (income.DueDate ?? income.Date).Date;

    public static DateTime EffectiveDueDate(TransactionResponse expense) => (expense.DueDate ?? expense.Date).Date;

    public static Result Compute(IReadOnlyList<TransactionResponse> incomes, IReadOnlyList<TransactionResponse> expenses)
    {
        var ordered = incomes
            .Where(i => i.Type == TransactionType.Income)
            .OrderBy(CashDate)
            .ThenBy(i => i.Amount)
            .ToList();

        var paidWith = new Dictionary<Guid, TransactionResponse>();
        var totals = ordered.ToDictionary(i => i.Id, _ => (total: 0m, count: 0));
        var unassignedTotal = 0m;
        var unassignedCount = 0;

        foreach (var expense in expenses.Where(e => e.Type == TransactionType.Expense))
        {
            var due = EffectiveDueDate(expense);
            var income = ordered.LastOrDefault(i => CashDate(i) <= due);
            if (income == null)
            {
                unassignedTotal += expense.Amount;
                unassignedCount++;
                continue;
            }

            paidWith[expense.Id] = income;
            var (total, count) = totals[income.Id];
            totals[income.Id] = (total + expense.Amount, count + 1);
        }

        var blocks = ordered
            .Select(i => new Block(i.Id, i.Description, i.Amount, CashDate(i), totals[i.Id].total, totals[i.Id].count, i.Amount - totals[i.Id].total))
            .ToList();

        Cycle? cycle = null;
        if (ordered.Count > 0)
        {
            var start = CashDate(ordered[0]);
            var end = start.AddMonths(1).AddDays(-1);
            cycle = new Cycle(start, end, string.Create(CultureInfo.InvariantCulture, $"{start:dd/MM}→{end:dd/MM}"));
        }

        return new Result(paidWith, blocks, unassignedTotal, unassignedCount, cycle);
    }
}
