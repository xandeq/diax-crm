using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public record TransactionResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date,
    TransactionType Type,
    RawBankType? RawBankType,
    string? RawDescription,
    string? Details,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    string? CategoryName,
    bool IsRecurring,
    bool IsSubscription,
    bool HasVariableAmount,
    Guid? FinancialAccountId,
    string? FinancialAccountName,
    Guid? CreditCardId,
    string? CreditCardName,
    Guid? CreditCardInvoiceId,
    TransactionStatus Status,
    DateTime? PaidDate,
    Guid? TransferGroupId,
    Guid? AccountTransferId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
