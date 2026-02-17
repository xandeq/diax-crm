namespace Diax.Domain.Finance;

/// <summary>
/// Status de uma transação financeira.
/// Renomeado de ExpenseStatus para uso unificado.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transação pendente / a pagar
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transação paga / confirmada
    /// </summary>
    Paid = 2
}
