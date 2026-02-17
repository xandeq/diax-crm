using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class ImportedTransaction : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public Guid StatementImportId { get; private set; }
    public string RawDescription { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public ImportTransactionStatus Status { get; private set; }

    // Novo: FK unificada para Transaction
    public Guid? CreatedTransactionId { get; private set; }
    public Guid? MatchedTransactionId { get; private set; }

    /// <summary>
    /// Tipo sugerido pelo sistema na importação (baseado em heurísticas).
    /// O usuário pode alterar antes de postar.
    /// </summary>
    public TransactionType? SuggestedType { get; private set; }

    // Legacy FKs (mantidas para período de migração)
    [Obsolete("Use CreatedTransactionId instead")]
    public Guid? MatchedExpenseId { get; private set; }
    [Obsolete("Use CreatedTransactionId instead")]
    public Guid? CreatedExpenseId { get; private set; }
    [Obsolete("Use CreatedTransactionId instead")]
    public Guid? CreatedIncomeId { get; private set; }

    public string? ErrorMessage { get; private set; }

    // Navigation properties
    public virtual StatementImport StatementImport { get; private set; } = null!;
    public virtual Transaction? CreatedTransaction { get; private set; }
    public virtual Transaction? MatchedTransaction { get; private set; }

    // Legacy navigation (mantidas para período de migração)
    [Obsolete("Use CreatedTransaction instead")]
    public virtual Expense? MatchedExpense { get; private set; }
    [Obsolete("Use CreatedTransaction instead")]
    public virtual Expense? CreatedExpense { get; private set; }
    [Obsolete("Use CreatedTransaction instead")]
    public virtual Income? CreatedIncome { get; private set; }

    private ImportedTransaction() { } // EF Core

    public static ImportedTransaction Create(
        Guid statementImportId,
        string rawDescription,
        decimal amount,
        DateTime transactionDate,
        Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        return new ImportedTransaction
        {
            StatementImportId = statementImportId,
            RawDescription = rawDescription,
            Amount = amount,
            TransactionDate = transactionDate,
            UserId = userId,
            Status = ImportTransactionStatus.Pending
        };
    }

    /// <summary>
    /// Define o tipo sugerido pelo sistema
    /// </summary>
    public void SetSuggestedType(TransactionType suggestedType)
    {
        SuggestedType = suggestedType;
    }

    public void MarkAsMatched(Guid transactionId)
    {
        MatchedTransactionId = transactionId;
        Status = ImportTransactionStatus.Matched;
    }

    [Obsolete("Use MarkAsMatched(Guid transactionId) instead")]
    public void MarkAsMatchedLegacy(Guid expenseId)
    {
        MatchedExpenseId = expenseId;
        Status = ImportTransactionStatus.Matched;
    }

    public void MarkAsCreated(Guid transactionId)
    {
        CreatedTransactionId = transactionId;
        Status = ImportTransactionStatus.Created;
    }

    [Obsolete("Use MarkAsCreated(Guid transactionId) instead")]
    public void MarkAsCreatedLegacy(Guid? expenseId = null, Guid? incomeId = null)
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
        CreatedTransactionId = null;
        MatchedTransactionId = null;
        MatchedExpenseId = null;
        CreatedExpenseId = null;
        CreatedIncomeId = null;
        SuggestedType = null;
        Status = ImportTransactionStatus.Pending;
        ErrorMessage = null;
    }
}
