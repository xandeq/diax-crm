namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Tipo de recomendação gerada pela simulação
/// </summary>
public enum RecommendationType
{
    /// <summary>
    /// Sugestão para adiar uma despesa
    /// </summary>
    DeferExpense = 1,

    /// <summary>
    /// Sugestão para trocar de cartão
    /// </summary>
    ChangeCard = 2,

    /// <summary>
    /// Sugestão para aumentar receita
    /// </summary>
    IncreaseIncome = 3,

    /// <summary>
    /// Alerta geral de risco
    /// </summary>
    Alert = 4,

    /// <summary>
    /// Otimização de pagamento
    /// </summary>
    OptimizePayment = 5
}
