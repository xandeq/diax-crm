namespace Diax.Domain.Finance;

/// <summary>
/// Define a qual tipo de transação uma categoria se aplica.
/// Permite categorias exclusivas de Income, Expense, ou ambas.
/// </summary>
public enum CategoryApplicableTo
{
    /// <summary>
    /// Categoria aplicável apenas a receitas
    /// </summary>
    Income = 1,

    /// <summary>
    /// Categoria aplicável apenas a despesas
    /// </summary>
    Expense = 2,

    /// <summary>
    /// Categoria aplicável a receitas e despesas (ex: Reembolso, Outros)
    /// </summary>
    Both = 3
}
