namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Status de uma transação projetada
/// </summary>
public enum ProjectedStatus
{
    /// <summary>
    /// Planejada - ainda não ocorreu
    /// </summary>
    Planned = 1,

    /// <summary>
    /// Confirmada - já ocorreu e foi registrada
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Cancelada - não vai mais ocorrer
    /// </summary>
    Cancelled = 3
}
