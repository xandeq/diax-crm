namespace Diax.Domain.Finance;

/// <summary>
/// Tipo financeiro (interpretado) da transação.
/// Diferente do tipo bancário bruto (RawBankType), este representa a classificação financeira real.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Receita real — dinheiro que entrou de fonte externa (salário, cliente, venda)
    /// </summary>
    Income = 1,

    /// <summary>
    /// Despesa real — dinheiro que saiu para gasto (aluguel, comida, serviço)
    /// </summary>
    Expense = 2,

    /// <summary>
    /// Transferência entre contas próprias — não é receita nem despesa.
    /// Impacto em receita/despesa global: ZERO.
    /// </summary>
    Transfer = 3,

    /// <summary>
    /// Transação ignorada — importada mas desconsiderada nos relatórios.
    /// Útil para lançamentos duplicados, tarifas já contabilizadas, etc.
    /// </summary>
    Ignored = 4
}
