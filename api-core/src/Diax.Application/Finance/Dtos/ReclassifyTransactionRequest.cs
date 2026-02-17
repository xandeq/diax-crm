using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

/// <summary>
/// Request para reclassificar o tipo financeiro de uma transação.
/// Ex: Mudar de Income para Transfer, ou de Expense para Ignored.
/// </summary>
public record ReclassifyTransactionRequest(
    TransactionType NewType,
    Guid? TransferGroupId = null
);
