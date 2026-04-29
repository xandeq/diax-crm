using Diax.Domain.Finance;

namespace Diax.Tests.Domain.Finance;

public class FinancialAccountTests
{
    private static FinancialAccount NewAccount(decimal initialBalance = 1000m)
    {
        return new FinancialAccount(
            name: "Conta Corrente",
            accountType: AccountType.Checking,
            initialBalance: initialBalance,
            userId: Guid.NewGuid());
    }

    [Fact]
    public void Constructor_SetsBalanceToInitialBalance()
    {
        var account = NewAccount(500m);
        Assert.Equal(500m, account.Balance);
        Assert.Equal(500m, account.InitialBalance);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var account = NewAccount(1000m);
        account.Credit(250.50m);
        Assert.Equal(1250.50m, account.Balance);
    }

    [Fact]
    public void Debit_DecreasesBalance()
    {
        var account = NewAccount(1000m);
        account.Debit(300m);
        Assert.Equal(700m, account.Balance);
    }

    [Fact]
    public void Debit_AllowsBalanceToGoNegative()
    {
        // Domain has no overdraft constraint; service layer is the gatekeeper.
        var account = NewAccount(100m);
        account.Debit(150m);
        Assert.Equal(-50m, account.Balance);
    }

    [Fact]
    public void CreditFollowedByDebit_ReverseAppliesCorrectDelta()
    {
        // Simulates ApplyBalanceImpact then ReverseBalanceImpact for an Income.
        var account = NewAccount(1000m);

        account.Credit(200m); // ApplyBalanceImpact for Income
        Assert.Equal(1200m, account.Balance);

        account.Debit(200m); // ReverseBalanceImpact for Income
        Assert.Equal(1000m, account.Balance);
    }

    [Fact]
    public void DebitFollowedByCredit_ReverseAppliesCorrectDelta()
    {
        // Simulates ApplyBalanceImpact then ReverseBalanceImpact for a non-card Expense.
        var account = NewAccount(1000m);

        account.Debit(150m); // ApplyBalanceImpact for Expense
        Assert.Equal(850m, account.Balance);

        account.Credit(150m); // ReverseBalanceImpact for Expense
        Assert.Equal(1000m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Credit_WithZeroOrNegative_Throws(decimal amount)
    {
        var account = NewAccount();
        Assert.Throws<ArgumentException>(() => account.Credit(amount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Debit_WithZeroOrNegative_Throws(decimal amount)
    {
        var account = NewAccount();
        Assert.Throws<ArgumentException>(() => account.Debit(amount));
    }

    [Fact]
    public void UpdateBalance_OverridesBalanceDirectly()
    {
        var account = NewAccount(1000m);
        account.UpdateBalance(2500.75m);
        Assert.Equal(2500.75m, account.Balance);
    }
}
