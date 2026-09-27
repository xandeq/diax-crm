using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// PaymentCycle mirrors the user's spreadsheet rule: an expense is paid with the last
/// income whose cash date is on/before the expense's real due date; the cycle runs from
/// the first cash date to the day before the next month's first cash date.
/// </summary>
public class PaymentCycleTests
{
    private static DateTime Utc(int y, int m, int d) => new(y, m, d, 12, 0, 0, DateTimeKind.Utc);

    private static TransactionResponse Tx(string name, decimal amount, DateTime date, TransactionType type, DateTime? dueDate = null)
        => new(Guid.NewGuid(), name, amount, date, type, null, null, null, PaymentMethod.Pix, null, null,
            true, false, false, null, null, null, null, null, TransactionStatus.Pending, null, null, null, null,
            DateTime.UtcNow, null, dueDate);

    private static TransactionResponse Income(string name, decimal amount, DateTime date, DateTime? cashDate = null)
        => Tx(name, amount, date, TransactionType.Income, cashDate);

    private static TransactionResponse Expense(string name, decimal amount, DateTime date, DateTime? dueDate = null)
        => Tx(name, amount, date, TransactionType.Expense, dueDate);

    // Setembro 2026 as in the sheet: KPIT 25/09, Niuro 01/10, Nortal 07/10, Pantheon 15/10, VA 24/10.
    private static List<TransactionResponse> SeptemberIncomes() =>
    [
        Income("NIURO", 18158.35m, Utc(2026, 9, 1), Utc(2026, 10, 1)),
        Income("NORTAL", 20752.40m, Utc(2026, 9, 7), Utc(2026, 10, 7)),
        Income("PANTHEON", 17795.48m, Utc(2026, 9, 15), Utc(2026, 10, 15)),
        Income("VA", 800m, Utc(2026, 9, 24), Utc(2026, 10, 24)),
        Income("KPIT", 8900m, Utc(2026, 9, 25)),
    ];

    [Fact]
    public void Compute_AssignsEachExpenseToLastIncomeOnOrBeforeDueDate()
    {
        var incomes = SeptemberIncomes();
        var safra = Expense("Safra", 3274m, Utc(2026, 9, 26));
        var aluguel = Expense("Aluguel", 9575.14m, Utc(2026, 9, 1), Utc(2026, 10, 1));
        var unimed = Expense("Unimed", 1450m, Utc(2026, 9, 10), Utc(2026, 10, 10));
        var latam = Expense("Latam", 6500m, Utc(2026, 9, 17), Utc(2026, 10, 17));

        var result = PaymentCycle.Compute(incomes, [safra, aluguel, unimed, latam]);

        Assert.Equal("KPIT", result.PaidWith[safra.Id].Description);
        Assert.Equal("NIURO", result.PaidWith[aluguel.Id].Description);
        Assert.Equal("NORTAL", result.PaidWith[unimed.Id].Description);
        Assert.Equal("PANTHEON", result.PaidWith[latam.Id].Description);
        Assert.Equal(0, result.UnassignedCount);
    }

    [Fact]
    public void Compute_ExpenseDueOnCashDate_BelongsToThatIncome()
    {
        var incomes = SeptemberIncomes();
        var drPedro = Expense("Dr. Pedro", 500m, Utc(2026, 9, 25));

        var result = PaymentCycle.Compute(incomes, [drPedro]);

        Assert.Equal("KPIT", result.PaidWith[drPedro.Id].Description);
    }

    [Fact]
    public void Compute_ExpenseBeforeFirstIncome_IsUnassigned()
    {
        var incomes = SeptemberIncomes();
        var early = Expense("Conta antiga", 100m, Utc(2026, 9, 10));

        var result = PaymentCycle.Compute(incomes, [early]);

        Assert.False(result.PaidWith.ContainsKey(early.Id));
        Assert.Equal(100m, result.UnassignedTotal);
        Assert.Equal(1, result.UnassignedCount);
    }

    [Fact]
    public void Compute_BlocksSumToTheCent()
    {
        var incomes = SeptemberIncomes();
        var expenses = new[]
        {
            Expense("Safra", 3274m, Utc(2026, 9, 26)),
            Expense("EDP", 689.27m, Utc(2026, 9, 30)),
            Expense("Aluguel", 9575.14m, Utc(2026, 9, 1), Utc(2026, 10, 1)),
            Expense("Condomínio", 1715.41m, Utc(2026, 9, 5), Utc(2026, 10, 5)),
            Expense("Unimed", 1450m, Utc(2026, 9, 10), Utc(2026, 10, 10)),
            Expense("Latam", 6500m, Utc(2026, 9, 17), Utc(2026, 10, 17)),
        };

        var result = PaymentCycle.Compute(incomes, expenses);

        var kpit = result.Blocks.Single(b => b.IncomeName == "KPIT");
        var niuro = result.Blocks.Single(b => b.IncomeName == "NIURO");
        var nortal = result.Blocks.Single(b => b.IncomeName == "NORTAL");
        var pantheon = result.Blocks.Single(b => b.IncomeName == "PANTHEON");
        var va = result.Blocks.Single(b => b.IncomeName == "VA");

        Assert.Equal(3963.27m, kpit.ExpensesTotal);
        Assert.Equal(8900m - 3963.27m, kpit.Balance);
        Assert.Equal(11290.55m, niuro.ExpensesTotal);
        Assert.Equal(1450m, nortal.ExpensesTotal);
        Assert.Equal(6500m, pantheon.ExpensesTotal);
        Assert.Equal(0m, va.ExpensesTotal);
        Assert.Equal(expenses.Sum(e => e.Amount), result.Blocks.Sum(b => b.ExpensesTotal) + result.UnassignedTotal);
        Assert.Equal(5, result.Blocks.Count);
    }

    [Fact]
    public void Compute_BlocksOrderedByCashDate()
    {
        var result = PaymentCycle.Compute(SeptemberIncomes(), []);

        Assert.Equal(["KPIT", "NIURO", "NORTAL", "PANTHEON", "VA"], result.Blocks.Select(b => b.IncomeName).ToArray());
        Assert.Equal(new DateTime(2026, 9, 25), result.Blocks[0].CashDate);
        Assert.Equal(new DateTime(2026, 10, 24), result.Blocks[4].CashDate);
    }

    [Fact]
    public void Compute_Cycle_RunsFromFirstCashDateToDayBeforeNextMonthsFirst()
    {
        var result = PaymentCycle.Compute(SeptemberIncomes(), []);

        Assert.NotNull(result.Cycle);
        Assert.Equal(new DateTime(2026, 9, 25), result.Cycle!.Start);
        Assert.Equal(new DateTime(2026, 10, 24), result.Cycle.End);
        Assert.Equal("25/09→24/10", result.Cycle.Label);
    }

    [Fact]
    public void Compute_Cycle_HandlesYearTurnAndShortMonths()
    {
        var dec = PaymentCycle.Compute([Income("KPIT", 1m, Utc(2026, 12, 25))], []);
        Assert.Equal(new DateTime(2027, 1, 24), dec.Cycle!.End);
        Assert.Equal("25/12→24/01", dec.Cycle.Label);

        var jan31 = PaymentCycle.Compute([Income("X", 1m, Utc(2027, 1, 31))], []);
        Assert.Equal(new DateTime(2027, 2, 27), jan31.Cycle!.End); // 31/01 + 1 mês = 28/02, véspera = 27/02

        var feb = PaymentCycle.Compute([Income("X", 1m, Utc(2028, 2, 29))], []);
        Assert.Equal(new DateTime(2028, 3, 28), feb.Cycle!.End);
    }

    [Fact]
    public void Compute_NoIncomes_NoCycleAndEverythingUnassigned()
    {
        var expense = Expense("Aluguel", 100m, Utc(2026, 9, 1));

        var result = PaymentCycle.Compute([], [expense]);

        Assert.Null(result.Cycle);
        Assert.Empty(result.Blocks);
        Assert.Equal(1, result.UnassignedCount);
    }

    [Fact]
    public void Compute_SameCashDate_PrefersLargerIncome()
    {
        var big = Income("Grande", 10000m, Utc(2026, 9, 25));
        var small = Income("Pequena", 100m, Utc(2026, 9, 25));
        var expense = Expense("Conta", 50m, Utc(2026, 9, 26));

        var result = PaymentCycle.Compute([small, big], [expense]);

        Assert.Equal(big.Id, result.PaidWith[expense.Id].Id);
    }

    [Fact]
    public void Compute_IgnoresNonExpenseAndNonIncomeRows()
    {
        var transfer = Tx("Transf", 999m, Utc(2026, 9, 26), TransactionType.Transfer);
        var result = PaymentCycle.Compute([transfer, Income("KPIT", 1m, Utc(2026, 9, 25))], [transfer]);

        Assert.Single(result.Blocks);
        Assert.Empty(result.PaidWith);
        Assert.Equal(0, result.UnassignedCount);
    }
}
