using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Recomendação gerada pela análise da simulação mensal
/// </summary>
public class SimulationRecommendation : AuditableEntity
{
    /// <summary>
    /// ID da simulação mensal
    /// </summary>
    public Guid SimulationId { get; set; }

    /// <summary>
    /// Tipo de recomendação
    /// </summary>
    public RecommendationType Type { get; set; }

    /// <summary>
    /// Prioridade da recomendação (1-10, sendo 1 a mais alta)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Título da recomendação
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem detalhada da recomendação
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID da transação projetada que pode ser ajustada (se aplicável)
    /// </summary>
    public Guid? ActionableTransactionId { get; set; }

    /// <summary>
    /// Navegação para a simulação
    /// </summary>
    public MonthlySimulation? Simulation { get; set; }

    /// <summary>
    /// Navegação para a transação acionável
    /// </summary>
    public ProjectedTransaction? ActionableTransaction { get; set; }

    /// <summary>
    /// Valor sugerido para ajuste (se aplicável)
    /// </summary>
    public decimal? SuggestedAmount { get; set; }

    /// <summary>
    /// Data sugerida para ajuste (se aplicável)
    /// </summary>
    public DateTime? SuggestedDate { get; set; }

    /// <summary>
    /// ID do cartão sugerido para troca (se aplicável)
    /// </summary>
    public Guid? SuggestedCreditCardId { get; set; }

    /// <summary>
    /// Verifica se a recomendação é crítica (alta prioridade)
    /// </summary>
    public bool IsCritical()
    {
        return Priority <= 3;
    }

    /// <summary>
    /// Verifica se a recomendação tem ação específica
    /// </summary>
    public bool IsActionable()
    {
        return ActionableTransactionId.HasValue ||
               SuggestedAmount.HasValue ||
               SuggestedDate.HasValue ||
               SuggestedCreditCardId.HasValue;
    }
}
