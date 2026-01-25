using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UploadStatementRequest(
    StatementImportType ImportType,
    Guid? FinancialAccountId,
    Guid? CreditCardGroupId
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
    string? FinancialAccountName,
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
    string? ErrorMessage
);

public record StatementImportDetailResponse(
    StatementImportResponse Summary,
    IEnumerable<ImportedTransactionResponse> Transactions
);
