namespace Diax.Application.Finance.Dtos;

public record UpdateAccountTransferRequest(
    Guid FromFinancialAccountId,
    Guid ToFinancialAccountId,
    decimal Amount,
    DateTime Date,
    string Description
);
