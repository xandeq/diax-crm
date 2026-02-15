namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Status de uma simulação mensal
/// </summary>
public enum SimulationStatus
{
    /// <summary>
    /// Rascunho - simulação em edição
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Ativa - simulação atual em uso
    /// </summary>
    Active = 2,

    /// <summary>
    /// Arquivada - simulação de mês passado
    /// </summary>
    Archived = 3
}
