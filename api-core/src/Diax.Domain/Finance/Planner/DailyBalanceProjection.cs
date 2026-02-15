using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Projeção de saldo para um dia específico dentro da simulação
/// </summary>
public class DailyBalanceProjection : AuditableEntity
{
    /// <summary>
    /// ID da simulação mensal
    /// </summary>
    public Guid SimulationId { get; set; }

    /// <summary>
    /// Data do dia
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Saldo de abertura (início do dia)
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Total de receitas do dia
    /// </summary>
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Total de despesas do dia
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Saldo de fechamento (fim do dia)
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Se o saldo ficou negativo neste dia
    /// </summary>
    public bool IsNegative { get; set; }

    /// <summary>
    /// Se há despesa de alta prioridade (1-20) neste dia
    /// </summary>
    public bool HasHighPriorityExpense { get; set; }

    /// <summary>
    /// Navegação para a simulação
    /// </summary>
    public MonthlySimulation? Simulation { get; set; }

    /// <summary>
    /// Calcula o nível de risco do dia
    /// </summary>
    public string GetRiskLevel()
    {
        if (IsNegative)
            return "Critical";

        if (ClosingBalance < 1000m)
            return "Warning";

        return "Safe";
    }

    /// <summary>
    /// Calcula a variação do dia (receitas - despesas)
    /// </summary>
    public decimal GetDayVariation()
    {
        return TotalIncome - TotalExpenses;
    }
}
