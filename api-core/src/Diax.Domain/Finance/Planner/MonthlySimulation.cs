using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Simulação mensal completa (projeção de saldo dia a dia + recomendações)
/// </summary>
public class MonthlySimulation : AuditableEntity, IUserOwnedEntity
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Mês de referência (1-12)
    /// </summary>
    public int ReferenceMonth { get; set; }

    /// <summary>
    /// Ano de referência
    /// </summary>
    public int ReferenceYear { get; set; }

    /// <summary>
    /// Data em que a simulação foi gerada
    /// </summary>
    public DateTime SimulationDate { get; set; }

    /// <summary>
    /// Saldo inicial do mês
    /// </summary>
    public decimal StartingBalance { get; set; }

    /// <summary>
    /// Saldo final projetado
    /// </summary>
    public decimal ProjectedEndingBalance { get; set; }

    /// <summary>
    /// Total de receitas projetadas
    /// </summary>
    public decimal TotalProjectedIncome { get; set; }

    /// <summary>
    /// Total de despesas projetadas
    /// </summary>
    public decimal TotalProjectedExpenses { get; set; }

    /// <summary>
    /// Se há risco de saldo negativo
    /// </summary>
    public bool HasNegativeBalanceRisk { get; set; }

    /// <summary>
    /// Primeiro dia em que o saldo fica negativo (se houver)
    /// </summary>
    public DateTime? FirstNegativeBalanceDate { get; set; }

    /// <summary>
    /// Menor saldo projetado no mês
    /// </summary>
    public decimal LowestProjectedBalance { get; set; }

    /// <summary>
    /// Status da simulação
    /// </summary>
    public SimulationStatus Status { get; set; }

    /// <summary>
    /// Transações projetadas para o mês
    /// </summary>
    public List<ProjectedTransaction> ProjectedTransactions { get; set; } = new();

    /// <summary>
    /// Saldos diários projetados
    /// </summary>
    public List<DailyBalanceProjection> DailyBalances { get; set; } = new();

    /// <summary>
    /// Recomendações geradas pela simulação
    /// </summary>
    public List<SimulationRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Recalcula os saldos diários baseado nas transações projetadas
    /// </summary>
    public void RecalculateDailyBalances()
    {
        DailyBalances.Clear();
        decimal currentBalance = StartingBalance;

        // Agrupar transações por dia
        var grouped = ProjectedTransactions
            .GroupBy(t => t.Date.Date)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var day in grouped)
        {
            decimal dayIncome = day
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            decimal dayExpenses = day
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var dailyBalance = new DailyBalanceProjection
            {
                SimulationId = Id,
                Date = day.Key,
                OpeningBalance = currentBalance,
                TotalIncome = dayIncome,
                TotalExpenses = dayExpenses,
                ClosingBalance = currentBalance + dayIncome - dayExpenses,
                IsNegative = (currentBalance + dayIncome - dayExpenses) < 0,
                HasHighPriorityExpense = day.Any(t => t.Type == TransactionType.Expense && t.Priority <= 20)
            };

            DailyBalances.Add(dailyBalance);
            currentBalance = dailyBalance.ClosingBalance;
        }

        // Atualizar campos da simulação
        ProjectedEndingBalance = currentBalance;
        LowestProjectedBalance = DailyBalances.Any()
            ? DailyBalances.Min(d => d.ClosingBalance)
            : StartingBalance;
        HasNegativeBalanceRisk = DailyBalances.Any(d => d.IsNegative);
        FirstNegativeBalanceDate = DailyBalances
            .FirstOrDefault(d => d.IsNegative)?.Date;
    }

    /// <summary>
    /// Detecta riscos financeiros na simulação
    /// </summary>
    public List<string> DetectRisks()
    {
        var risks = new List<string>();

        // Risco 1: Saldo negativo
        if (HasNegativeBalanceRisk && FirstNegativeBalanceDate.HasValue)
        {
            risks.Add($"Saldo negativo previsto em {FirstNegativeBalanceDate.Value:dd/MM/yyyy}");
        }

        // Risco 2: Abaixo do colchão mínimo (R$ 1.000)
        if (ProjectedEndingBalance < 1000)
        {
            risks.Add("Saldo final abaixo do colchão de segurança (R$ 1.000)");
        }

        // Risco 3: Faturas de cartão > 30% da receita
        decimal invoicesTotal = ProjectedTransactions
            .Where(t => t.Source == ProjectionSource.Invoice)
            .Sum(t => t.Amount);

        if (TotalProjectedIncome > 0 && invoicesTotal > TotalProjectedIncome * 0.3m)
        {
            risks.Add("Faturas de cartão excedem 30% da receita");
        }

        // Risco 4: Despesas > Receitas
        if (TotalProjectedExpenses > TotalProjectedIncome)
        {
            risks.Add("Despesas projetadas excedem receitas");
        }

        return risks;
    }

    /// <summary>
    /// Calcula o valor de sobra disponível (saldo final - colchão mínimo)
    /// </summary>
    public decimal GetSurplusAmount()
    {
        const decimal minimumCushion = 1000m; // R$ 1.000
        var surplus = ProjectedEndingBalance - minimumCushion;
        return surplus > 0 ? surplus : 0;
    }

    /// <summary>
    /// Aloca sobras para metas financeiras ativas (será implementado no serviço)
    /// </summary>
    public void AllocateSurplusToGoals()
    {
        // Implementação será feita no MonthlySimulationService
        // que tem acesso ao repositório de FinancialGoals
    }
}
