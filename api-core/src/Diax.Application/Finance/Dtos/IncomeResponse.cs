using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record IncomeResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    Guid IncomeCategoryId,
    string? IncomeCategoryName,
    bool IsRecurring,
    Guid FinancialAccountId,
    string? FinancialAccountName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
