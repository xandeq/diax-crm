using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UpdateFinancialAccountRequest(
    string Name,
    AccountType AccountType,
    bool IsActive
);
