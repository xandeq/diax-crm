using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record UpdateIncomeRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    PaymentMethod PaymentMethod,
    string? Category,
    bool IsRecurring
);
