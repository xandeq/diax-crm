using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Meta financeira do usuário (ex: Reserva para bebê, Emergência, Viagem)
/// </summary>
public class FinancialGoal : AuditableEntity, IUserOwnedEntity
{
    /// <summary>
    /// ID do usuário dono da meta
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Nome da meta (ex: "Reserva Bebê Abril/2026")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Valor total da meta
    /// </summary>
    public decimal TargetAmount { get; set; }

    /// <summary>
    /// Valor já acumulado
    /// </summary>
    public decimal CurrentAmount { get; set; }

    /// <summary>
    /// Data alvo para atingir a meta (opcional)
    /// </summary>
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Categoria da meta
    /// </summary>
    public GoalCategory Category { get; set; }

    /// <summary>
    /// Prioridade (1-10, sendo 1 a mais alta)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Se a meta está ativa
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Se deve alocar sobras automaticamente para esta meta
    /// </summary>
    public bool AutoAllocateSurplus { get; set; }

    /// <summary>
    /// Adiciona uma contribuição à meta
    /// </summary>
    public void AddContribution(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("O valor da contribuição deve ser positivo", nameof(amount));

        CurrentAmount += amount;

        // Não permitir ultrapassar a meta
        if (CurrentAmount > TargetAmount)
            CurrentAmount = TargetAmount;
    }

    /// <summary>
    /// Calcula o progresso da meta em percentual
    /// </summary>
    public decimal GetProgress()
    {
        if (TargetAmount == 0)
            return 0;

        return Math.Round((CurrentAmount / TargetAmount) * 100, 2);
    }

    /// <summary>
    /// Calcula quanto falta para atingir a meta
    /// </summary>
    public decimal GetRemainingAmount()
    {
        var remaining = TargetAmount - CurrentAmount;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Verifica se a meta já foi atingida
    /// </summary>
    public bool IsCompleted()
    {
        return CurrentAmount >= TargetAmount;
    }
}
