using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Transação projetada dentro de uma simulação mensal
/// </summary>
public class ProjectedTransaction : AuditableEntity
{
    /// <summary>
    /// ID da simulação mensal
    /// </summary>
    public Guid SimulationId { get; set; }

    /// <summary>
    /// Tipo da transação (Receita ou Despesa)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Descrição da transação
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor da transação
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Data da transação
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// ID da categoria
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Nome da categoria (desnormalizado para performance)
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Método de pagamento
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Prioridade (1-100, sendo 1 a mais alta)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Origem da projeção
    /// </summary>
    public ProjectionSource Source { get; set; }

    /// <summary>
    /// ID da entidade de origem (RecurringTransaction, Expense, Income, etc.)
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// ID do cartão de crédito (se aplicável)
    /// </summary>
    public Guid? CreditCardId { get; set; }

    /// <summary>
    /// ID da conta financeira (se aplicável)
    /// </summary>
    public Guid? FinancialAccountId { get; set; }

    /// <summary>
    /// Status da transação projetada
    /// </summary>
    public ProjectedStatus Status { get; set; }

    /// <summary>
    /// ID da transação real (Expense/Income) se já foi confirmada
    /// </summary>
    public Guid? ActualTransactionId { get; set; }

    /// <summary>
    /// Navegação para a simulação
    /// </summary>
    public MonthlySimulation? Simulation { get; set; }

    /// <summary>
    /// Marca a transação projetada como confirmada
    /// </summary>
    public void MarkAsConfirmed()
    {
        Status = ProjectedStatus.Confirmed;
    }

    /// <summary>
    /// Vincula a transação projetada com a transação real
    /// </summary>
    public void MatchWithActualTransaction(Guid actualId)
    {
        ActualTransactionId = actualId;
        Status = ProjectedStatus.Confirmed;
    }

    /// <summary>
    /// Cancela a transação projetada
    /// </summary>
    public void Cancel()
    {
        Status = ProjectedStatus.Cancelled;
    }
}
