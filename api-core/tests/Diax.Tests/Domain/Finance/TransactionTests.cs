using Diax.Domain.Finance;

namespace Diax.Tests.Domain.Finance;

public class TransactionTests
{
    [Fact]
    public void CreateExpense_WithCreditCardPayment_ShouldRequireCreditCardId()
    {
        var act = () => Transaction.CreateExpense(
            description: "Invoice",
            amount: 120m,
            date: DateTime.UtcNow,
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: Guid.NewGuid(),
            creditCardId: null,
            creditCardInvoiceId: null,
            financialAccountId: null);

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("CreditCardId", exception.Message);
    }

    [Fact]
    public void CreateExpense_WithDebitPayment_ShouldRequireFinancialAccount()
    {
        var act = () => Transaction.CreateExpense(
            description: "Utility bill",
            amount: 120m,
            date: DateTime.UtcNow,
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: Guid.NewGuid(),
            creditCardId: null,
            creditCardInvoiceId: null,
            financialAccountId: null);

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("FinancialAccountId", exception.Message);
    }

    [Fact]
    public void MarkAsPaid_AndMarkAsPending_ShouldUpdateStatusAndPaidDate()
    {
        var transaction = Transaction.CreateExpense(
            description: "Phone bill",
            amount: 90m,
            date: DateTime.UtcNow,
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: Guid.NewGuid(),
            financialAccountId: Guid.NewGuid());

        transaction.MarkAsPaid();
        Assert.Equal(TransactionStatus.Paid, transaction.Status);
        Assert.NotNull(transaction.PaidDate);

        transaction.MarkAsPending();
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
        Assert.Null(transaction.PaidDate);
    }
}
