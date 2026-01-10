namespace Diax.Application.Finance.Dtos;

public record AccountTransferResponse(
    Guid Id,
    Guid FromFinancialAccountId,
    string FromAccountName,
    Guid ToFinancialAccountId,
    string ToAccountName,
    decimal Amount,
    DateTime Date,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
