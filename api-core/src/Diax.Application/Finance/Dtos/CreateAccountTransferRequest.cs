namespace Diax.Application.Finance.Dtos;

public record CreateAccountTransferRequest(
    Guid FromFinancialAccountId,
    Guid ToFinancialAccountId,
    decimal Amount,
    DateTime Date,
    string Description
);
