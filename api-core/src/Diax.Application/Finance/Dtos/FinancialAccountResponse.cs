using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record FinancialAccountResponse(
    Guid Id,
    string Name,
    AccountType AccountType,
    decimal InitialBalance,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
