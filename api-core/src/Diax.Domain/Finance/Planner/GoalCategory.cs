namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Categoria de meta financeira
/// </summary>
public enum GoalCategory
{
    /// <summary>
    /// Reserva de emergência
    /// </summary>
    Emergency = 1,

    /// <summary>
    /// Meta para bebê (ex: reserva para nascimento)
    /// </summary>
    Baby = 2,

    /// <summary>
    /// Meta para casa (compra, reforma, etc.)
    /// </summary>
    House = 3,

    /// <summary>
    /// Meta para viagem
    /// </summary>
    Travel = 4,

    /// <summary>
    /// Meta de investimento
    /// </summary>
    Investment = 5,

    /// <summary>
    /// Pagamento de dívida
    /// </summary>
    Debt = 6,

    /// <summary>
    /// Outras categorias
    /// </summary>
    Other = 99
}
