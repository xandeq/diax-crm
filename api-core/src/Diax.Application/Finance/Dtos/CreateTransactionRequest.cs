using System.ComponentModel.DataAnnotations;
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
    [property: Required, StringLength(500, MinimumLength = 1)] string Description,
    [property: Range(typeof(decimal), "0.01", "999999999.99")] decimal Amount,
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
    [property: StringLength(2000)] string? Details = null,
    bool IsSubscription = false,
    bool HasVariableAmount = false
);
