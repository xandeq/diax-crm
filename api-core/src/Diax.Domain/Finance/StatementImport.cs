using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class StatementImport : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public StatementImportType ImportType { get; private set; }
    public Guid? FinancialAccountId { get; private set; }
    public Guid? CreditCardGroupId { get; private set; }
    public Guid? CreditCardId { get; private set; } // New property
    public string FileName { get; private set; } = string.Empty;
    public string FileContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public ImportStatus Status { get; private set; }
    public int TotalRecords { get; private set; }
    public int ProcessedRecords { get; private set; }
    public int FailedRecords { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // Navigation properties
    public virtual FinancialAccount? FinancialAccount { get; private set; }
    public virtual CreditCardGroup? CreditCardGroup { get; private set; }
    public virtual CreditCard? CreditCard { get; private set; } // New navigation
    public virtual ICollection<ImportedTransaction> Transactions { get; private set; } = new List<ImportedTransaction>();

    private StatementImport() { } // EF Core

    public static StatementImport Create(
        StatementImportType importType,
        string fileName,
        string fileContentType,
        long fileSize,
        Guid userId,
        Guid? financialAccountId = null,
        Guid? creditCardGroupId = null,
        Guid? creditCardId = null) // New parameter
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        return new StatementImport
        {
            ImportType = importType,
            FileName = fileName,
            FileContentType = fileContentType,
            FileSize = fileSize,
            UserId = userId,
            FinancialAccountId = financialAccountId,
            CreditCardGroupId = creditCardGroupId,
            CreditCardId = creditCardId, // Set property
            Status = ImportStatus.Pending
        };
    }

    public void StartProcessing()
    {
        Status = ImportStatus.Processing;
    }

    public void Complete(int total, int processed, int failed)
    {
        TotalRecords = total;
        ProcessedRecords = processed;
        FailedRecords = failed;
        Status = ImportStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ImportStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }
}
