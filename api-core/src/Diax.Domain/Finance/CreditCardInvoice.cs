using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Representa uma fatura de cartão de crédito em um período específico
/// </summary>
public class CreditCardInvoice : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public Guid CreditCardGroupId { get; private set; }
    public virtual CreditCardGroup CreditCardGroup { get; private set; } = null!;

    public int ReferenceMonth { get; private set; }
    public int ReferenceYear { get; private set; }
    public DateTime ClosingDate { get; private set; }
    public DateTime DueDate { get; private set; }

    public bool IsPaid { get; private set; }
    public DateTime? PaymentDate { get; private set; }

    public Guid? PaidFromAccountId { get; private set; }
    public virtual FinancialAccount? PaidFromAccount { get; private set; }

    public decimal? StatementAmount { get; private set; }

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    [Obsolete("Use Transactions instead")]
    public virtual ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    protected CreditCardInvoice() { }

    public CreditCardInvoice(
        Guid creditCardGroupId,
        int referenceMonth,
        int referenceYear,
        DateTime closingDate,
        DateTime dueDate,
        Guid userId)
    {
        if (referenceMonth < 1 || referenceMonth > 12)
            throw new ArgumentException("Month must be between 1 and 12", nameof(referenceMonth));

        if (referenceYear < 2000)
            throw new ArgumentException("Year must be 2000 or later", nameof(referenceYear));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        CreditCardGroupId = creditCardGroupId;
        ReferenceMonth = referenceMonth;
        ReferenceYear = referenceYear;
        ClosingDate = closingDate;
        DueDate = dueDate;
        UserId = userId;
        IsPaid = false;
    }

    /// <summary>
    /// Calcula o valor total da fatura baseado nas transações e despesas legadas vinculadas.
    /// </summary>
    public decimal GetTotalAmount()
    {
        var transactionsTotal = Transactions?
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount) ?? 0m;

#pragma warning disable CS0618
        var legacyExpensesTotal = Expenses?.Sum(e => e.Amount) ?? 0m;
#pragma warning restore CS0618

        return transactionsTotal + legacyExpensesTotal;
    }

    /// <summary>
    /// Define o valor da fatura conforme extrato
    /// </summary>
    public void SetStatementAmount(decimal? amount)
    {
        StatementAmount = amount;
    }

    /// <summary>
    /// Marca a fatura como paga
    /// </summary>
    public void MarkAsPaid(DateTime paymentDate, Guid? paidFromAccountId = null, decimal? statementAmount = null)
    {
        IsPaid = true;
        PaymentDate = paymentDate;
        PaidFromAccountId = paidFromAccountId;
        if (statementAmount.HasValue) StatementAmount = statementAmount;
    }

    /// <summary>
    /// Desmarca a fatura como paga
    /// </summary>
    public void MarkAsUnpaid()
    {
        IsPaid = false;
        PaymentDate = null;
        PaidFromAccountId = null;
    }
}
