using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

/// <summary>
/// Request para criar uma transação unificada.
/// O campo Type determina as regras de validação:
/// - Income: FinancialAccountId obrigatório, CreditCardId proibido
/// - Expense (CreditCard): CreditCardId obrigatório, FinancialAccountId proibido
/// - Expense (outros): FinancialAccountId obrigatório, CreditCardId proibido
/// - Transfer: FinancialAccountId obrigatório, gerido via AccountTransferService
/// </summary>
public record CreateTransactionRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    Guid? CategoryId,
    bool IsRecurring,
    Guid? FinancialAccountId = null,
    Guid? CreditCardId = null,
    Guid? CreditCardInvoiceId = null,
    TransactionStatus Status = TransactionStatus.Pending,
    DateTime? PaidDate = null,
    string? Details = null,
    bool IsSubscription = false
);
