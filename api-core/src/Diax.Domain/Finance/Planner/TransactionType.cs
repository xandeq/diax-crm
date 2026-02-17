namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Tipo de transação usado no Planner.
/// DEPRECATED: Use Diax.Domain.Finance.TransactionType diretamente.
/// Mantido para compatibilidade durante migração.
/// </summary>
[Obsolete("Use Diax.Domain.Finance.TransactionType instead")]
public enum TransactionType
{
    /// <summary>
    /// Receita/Entrada
    /// </summary>
    Income = 1,

    /// <summary>
    /// Despesa/Saída
    /// </summary>
    Expense = 2
}
