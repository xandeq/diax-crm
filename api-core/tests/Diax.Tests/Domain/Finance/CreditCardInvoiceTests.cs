using Diax.Domain.Finance;

namespace Diax.Tests.Domain.Finance;

public class CreditCardInvoiceTests
{
    private static CreditCardInvoice NewInvoice(Guid userId)
    {
        return new CreditCardInvoice(
            creditCardGroupId: Guid.NewGuid(),
            referenceMonth: 4,
            referenceYear: 2026,
            closingDate: new DateTime(2026, 4, 25),
            dueDate: new DateTime(2026, 5, 5),
            userId: userId);
    }

    private static Transaction NewCardExpense(Guid userId, Guid creditCardId, Guid invoiceId, decimal amount)
    {
        return Transaction.CreateExpense(
            description: $"Card expense {amount:F2}",
            amount: amount,
            date: new DateTime(2026, 4, 10),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId,
            creditCardInvoiceId: invoiceId,
            financialAccountId: null);
    }

    [Fact]
    public void GetTotalAmount_WithEmptyCollections_ReturnsZero()
    {
        var invoice = NewInvoice(Guid.NewGuid());
        Assert.Equal(0m, invoice.GetTotalAmount());
    }

    [Fact]
    public void GetTotalAmount_WithOnlyTransactions_SumsTransactions()
    {
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var invoice = NewInvoice(userId);

        invoice.Transactions.Add(NewCardExpense(userId, creditCardId, Guid.NewGuid(), 100m));
        invoice.Transactions.Add(NewCardExpense(userId, creditCardId, Guid.NewGuid(), 250.55m));

        Assert.Equal(350.55m, invoice.GetTotalAmount());
    }

#pragma warning disable CS0618 // Legacy Expense / Expenses collection used intentionally for transition coverage.
    [Fact]
    public void GetTotalAmount_WithOnlyLegacyExpenses_SumsExpenses()
    {
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var invoice = NewInvoice(userId);

        invoice.Expenses.Add(new Expense(
            description: "Legacy 1",
            amount: 80m,
            date: new DateTime(2026, 4, 12),
            paymentMethod: PaymentMethod.CreditCard,
            expenseCategoryId: categoryId,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId));

        invoice.Expenses.Add(new Expense(
            description: "Legacy 2",
            amount: 19.99m,
            date: new DateTime(2026, 4, 14),
            paymentMethod: PaymentMethod.CreditCard,
            expenseCategoryId: categoryId,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId));

        Assert.Equal(99.99m, invoice.GetTotalAmount());
    }

    [Fact]
    public void GetTotalAmount_WithMixedTransactionsAndExpenses_SumsBoth()
    {
        // Regression test for commit 6c60317: invoice unification (fd9eee0) created
        // a window where invoices that mixed pre-unification Expenses with new
        // Transactions returned only the Expense subtotal. GetTotalAmount must sum both.
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var invoice = NewInvoice(userId);

        invoice.Transactions.Add(NewCardExpense(userId, creditCardId, Guid.NewGuid(), 200m));
        invoice.Transactions.Add(NewCardExpense(userId, creditCardId, Guid.NewGuid(), 50.50m));

        invoice.Expenses.Add(new Expense(
            description: "Legacy",
            amount: 49.50m,
            date: new DateTime(2026, 4, 12),
            paymentMethod: PaymentMethod.CreditCard,
            expenseCategoryId: categoryId,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId));

        Assert.Equal(300m, invoice.GetTotalAmount());
    }
#pragma warning restore CS0618

    [Fact]
    public void GetTotalAmount_IgnoresNonExpenseTransactions()
    {
        // The invoice navigation only links Expense-type Transactions in production,
        // but defensive code filters by Type=Expense. Validate the filter so a future
        // schema slip (e.g. a refund modeled as Income) cannot inflate the invoice total.
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var invoice = NewInvoice(userId);
        var invoiceId = Guid.NewGuid();

        invoice.Transactions.Add(NewCardExpense(userId, creditCardId, invoiceId, 120m));

        // Income transaction wrongly attached to invoice (defensive filter must drop it).
        var rogueIncome = Transaction.CreateIncome(
            description: "Refund credited to bank",
            amount: 999m,
            date: new DateTime(2026, 4, 15),
            paymentMethod: PaymentMethod.BankTransfer,
            categoryId: null,
            isRecurring: false,
            financialAccountId: Guid.NewGuid(),
            userId: userId);
        invoice.Transactions.Add(rogueIncome);

        Assert.Equal(120m, invoice.GetTotalAmount());
    }
}
