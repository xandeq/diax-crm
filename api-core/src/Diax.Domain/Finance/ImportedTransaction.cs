using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class ImportedTransaction : AuditableEntity
{
    public Guid StatementImportId { get; private set; }
    public string RawDescription { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public ImportTransactionStatus Status { get; private set; }
    public Guid? MatchedExpenseId { get; private set; }
    public Guid? CreatedExpenseId { get; private set; }
    public Guid? CreatedIncomeId { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation properties
    public virtual StatementImport StatementImport { get; private set; } = null!;
    public virtual Expense? MatchedExpense { get; private set; }
    public virtual Expense? CreatedExpense { get; private set; }
    public virtual Income? CreatedIncome { get; private set; }

    private ImportedTransaction() { } // EF Core

    public static ImportedTransaction Create(
        Guid statementImportId,
        string rawDescription,
        decimal amount,
        DateTime transactionDate)
    {
        return new ImportedTransaction
        {
            StatementImportId = statementImportId,
            RawDescription = rawDescription,
            Amount = amount,
            TransactionDate = transactionDate,
            Status = ImportTransactionStatus.Pending
        };
    }

    public void MarkAsMatched(Guid expenseId)
    {
        MatchedExpenseId = expenseId;
        Status = ImportTransactionStatus.Matched;
    }

    public void MarkAsCreated(Guid? expenseId = null, Guid? incomeId = null)
    {
        CreatedExpenseId = expenseId;
        CreatedIncomeId = incomeId;
        Status = ImportTransactionStatus.Created;
    }

    public void MarkAsSkipped(string? reason = null)
    {
        Status = ImportTransactionStatus.Skipped;
        if (!string.IsNullOrEmpty(reason))
        {
            ErrorMessage = reason;
        }
    }

    public void MarkAsError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = ImportTransactionStatus.Error;
    }

    public void Reset()
    {
        MatchedExpenseId = null;
        CreatedExpenseId = null;
        CreatedIncomeId = null;
        Status = ImportTransactionStatus.Pending;
        ErrorMessage = null;
    }
}
