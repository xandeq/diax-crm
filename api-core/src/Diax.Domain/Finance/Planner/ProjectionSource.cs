namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Origem de uma transação projetada
/// </summary>
public enum ProjectionSource
{
    /// <summary>
    /// Baseada em RecurringTransaction
    /// </summary>
    Recurring = 1,

    /// <summary>
    /// Baseada em média histórica
    /// </summary>
    Historical = 2,

    /// <summary>
    /// Adicionada manualmente pelo usuário
    /// </summary>
    Manual = 3,

    /// <summary>
    /// Baseada em fatura de cartão de crédito
    /// </summary>
    Invoice = 4,

    /// <summary>
    /// Baseada em Expense/Income já confirmada
    /// </summary>
    Confirmed = 5
}
