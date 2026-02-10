using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UploadStatementRequest(
    StatementImportType ImportType,
    Guid? FinancialAccountId,
    Guid? CreditCardGroupId,
    Guid? CreditCardId // New property
);

public record StatementImportResponse(
    Guid Id,
    StatementImportType ImportType,
    string FileName,
    ImportStatus Status,
    int TotalRecords,
    int ProcessedRecords,
    int FailedRecords,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    Guid? FinancialAccountId,
    string? FinancialAccountName,
    Guid? CreditCardGroupId,
    string? CreditCardGroupName
);

public record ImportedTransactionResponse(
    Guid Id,
    string RawDescription,
    decimal Amount,
    DateTime TransactionDate,
    ImportTransactionStatus Status,
    Guid? MatchedExpenseId,
    Guid? CreatedExpenseId,
    Guid? CreatedIncomeId,
    string? ErrorMessage
);

public record StatementImportDetailResponse(
    StatementImportResponse Summary,
    IEnumerable<ImportedTransactionResponse> Transactions
);

public record StatementImportPostPreviewResponse(
    int Total,
    int ExpensesToCreate,
    int IncomesToCreate,
    int AlreadyCreated,
    int ToIgnore,
    int Failed
);

public record StatementImportPostRequest(
    bool Force = false
);

public record StatementImportPostResponse(
    int CreatedExpenses,
    int CreatedIncomes,
    int Skipped,
    int Failed
);
