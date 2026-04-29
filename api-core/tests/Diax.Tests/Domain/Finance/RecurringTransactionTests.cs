using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Tests.Domain.Finance;

public class RecurringTransactionTests
{
    private static RecurringTransaction NewMonthly(int dayOfMonth, DateTime startDate, DateTime? endDate = null)
    {
        return new RecurringTransaction
        {
            UserId = Guid.NewGuid(),
            Type = PlannerTransactionType.Expense,
            Description = "Aluguel",
            Amount = 1500m,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Monthly,
            DayOfMonth = dayOfMonth,
            StartDate = startDate,
            EndDate = endDate,
            PaymentMethod = PaymentMethod.DebitCard,
            FinancialAccountId = Guid.NewGuid(),
            IsActive = true,
            Priority = 1
        };
    }

    [Fact]
    public void GetNextOccurrences_Monthly_Day31_ClampsToFebruary28InNonLeapYear()
    {
        var template = NewMonthly(dayOfMonth: 31, startDate: new DateTime(2026, 1, 1));

        var occurrences = template.GetNextOccurrences(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 4, 30));

        Assert.Equal(4, occurrences.Count);
        Assert.Equal(new DateTime(2026, 1, 31), occurrences[0]);
        Assert.Equal(new DateTime(2026, 2, 28), occurrences[1]); // 2026 is not a leap year
        Assert.Equal(new DateTime(2026, 3, 31), occurrences[2]);
        Assert.Equal(new DateTime(2026, 4, 30), occurrences[3]);
    }

    [Fact]
    public void GetNextOccurrences_Monthly_Day31_ClampsToFebruary29InLeapYear()
    {
        var template = NewMonthly(dayOfMonth: 31, startDate: new DateTime(2024, 1, 1));

        var occurrences = template.GetNextOccurrences(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 3, 31));

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2024, 1, 31), occurrences[0]);
        Assert.Equal(new DateTime(2024, 2, 29), occurrences[1]); // 2024 is a leap year
        Assert.Equal(new DateTime(2024, 3, 31), occurrences[2]);
    }

    [Fact]
    public void GetNextOccurrences_Monthly_Day15_RemainsConsistent()
    {
        var template = NewMonthly(dayOfMonth: 15, startDate: new DateTime(2026, 1, 1));

        var occurrences = template.GetNextOccurrences(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 6, 30));

        Assert.Equal(6, occurrences.Count);
        Assert.All(occurrences, d => Assert.Equal(15, d.Day));
    }

    [Fact]
    public void GetNextOccurrences_Monthly_RespectsStartDate()
    {
        // Window starts before the template's StartDate — those months must be skipped.
        var template = NewMonthly(dayOfMonth: 10, startDate: new DateTime(2026, 3, 10));

        var occurrences = template.GetNextOccurrences(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 5, 31));

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 3, 10), occurrences[0]);
        Assert.Equal(new DateTime(2026, 4, 10), occurrences[1]);
        Assert.Equal(new DateTime(2026, 5, 10), occurrences[2]);
    }

    [Fact]
    public void GetNextOccurrences_Monthly_RespectsEndDate()
    {
        var template = NewMonthly(
            dayOfMonth: 5,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 3, 31));

        var occurrences = template.GetNextOccurrences(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 3, 5), occurrences.Last());
    }

    [Fact]
    public void GetNextOccurrences_Yearly_Day29Feb_ClampsToFeb28InNonLeapYear()
    {
        var template = new RecurringTransaction
        {
            UserId = Guid.NewGuid(),
            Type = PlannerTransactionType.Expense,
            Description = "Anuidade",
            Amount = 200m,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Yearly,
            DayOfMonth = 29,
            StartDate = new DateTime(2024, 2, 29), // leap day
            PaymentMethod = PaymentMethod.DebitCard,
            FinancialAccountId = Guid.NewGuid(),
            IsActive = true,
            Priority = 1
        };

        var occurrences = template.GetNextOccurrences(
            new DateTime(2024, 1, 1),
            new DateTime(2027, 12, 31));

        Assert.Equal(4, occurrences.Count);
        Assert.Equal(new DateTime(2024, 2, 29), occurrences[0]); // leap year
        Assert.Equal(new DateTime(2025, 2, 28), occurrences[1]); // clamped
        Assert.Equal(new DateTime(2026, 2, 28), occurrences[2]); // clamped
        Assert.Equal(new DateTime(2027, 2, 28), occurrences[3]); // clamped
    }

    [Fact]
    public void IsApplicableForMonth_BeforeStartDate_ReturnsFalse()
    {
        var template = NewMonthly(dayOfMonth: 10, startDate: new DateTime(2026, 5, 1));

        Assert.False(template.IsApplicableForMonth(month: 4, year: 2026));
        Assert.True(template.IsApplicableForMonth(month: 5, year: 2026));
    }

    [Fact]
    public void IsApplicableForMonth_AfterEndDate_ReturnsFalse()
    {
        var template = NewMonthly(
            dayOfMonth: 10,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 3, 31));

        Assert.True(template.IsApplicableForMonth(month: 3, year: 2026));
        Assert.False(template.IsApplicableForMonth(month: 4, year: 2026));
    }
}
