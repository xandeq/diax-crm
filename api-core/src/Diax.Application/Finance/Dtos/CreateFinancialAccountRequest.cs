using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record CreateFinancialAccountRequest(
    string Name,
    AccountType AccountType,
    decimal InitialBalance,
    bool IsActive = true
);
