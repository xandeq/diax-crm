using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Template de transação recorrente (ex: Aluguel todo dia 5, Salário todo dia 10)
/// </summary>
public class RecurringTransaction : AuditableEntity, IUserOwnedEntity
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tipo da transação (Receita ou Despesa)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Descrição da transação recorrente
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor da transação
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID da categoria (Income ou Expense)
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Frequência da recorrência
    /// </summary>
    public FrequencyType FrequencyType { get; set; }

    /// <summary>
    /// Dia do mês (1-31) para recorrências mensais
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Data de início da recorrência
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Data de fim da recorrência (opcional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Método de pagamento
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// ID do cartão de crédito (se PaymentMethod = CreditCard)
    /// </summary>
    public Guid? CreditCardId { get; set; }

    /// <summary>
    /// ID da conta financeira (se PaymentMethod != CreditCard)
    /// </summary>
    public Guid? FinancialAccountId { get; set; }

    /// <summary>
    /// Se a recorrência está ativa
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Prioridade (1-100, sendo 1 a mais alta)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Verifica se a recorrência se aplica a um mês/ano específico
    /// </summary>
    public bool IsApplicableForMonth(int month, int year)
    {
        var targetDate = new DateTime(year, month, 1);

        // Verifica se está dentro do período de validade
        if (targetDate < StartDate.Date)
            return false;

        if (EndDate.HasValue && targetDate > EndDate.Value.Date)
            return false;

        return true;
    }

    /// <summary>
    /// Calcula as próximas ocorrências dentro de um período
    /// </summary>
    public List<DateTime> GetNextOccurrences(DateTime startDate, DateTime endDate)
    {
        var occurrences = new List<DateTime>();

        switch (FrequencyType)
        {
            case FrequencyType.Monthly:
                var current = new DateTime(startDate.Year, startDate.Month, 1);
                while (current <= endDate)
                {
                    // Verifica se o dia existe no mês
                    var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                    var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;

                    var occurrence = new DateTime(current.Year, current.Month, day);

                    if (occurrence >= startDate && occurrence <= endDate && IsApplicableForMonth(current.Month, current.Year))
                    {
                        occurrences.Add(occurrence);
                    }

                    current = current.AddMonths(1);
                }
                break;

            case FrequencyType.Quarterly:
                current = new DateTime(startDate.Year, startDate.Month, 1);
                while (current <= endDate)
                {
                    var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                    var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;
                    var occurrence = new DateTime(current.Year, current.Month, day);

                    if (occurrence >= startDate && occurrence <= endDate && IsApplicableForMonth(current.Month, current.Year))
                    {
                        occurrences.Add(occurrence);
                    }

                    current = current.AddMonths(3);
                }
                break;

            case FrequencyType.Yearly:
                for (int year = startDate.Year; year <= endDate.Year; year++)
                {
                    // Para yearly, usar o mês de StartDate
                    var month = StartDate.Month;
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;
                    var occurrence = new DateTime(year, month, day);

                    if (occurrence >= startDate && occurrence <= endDate && IsApplicableForMonth(month, year))
                    {
                        occurrences.Add(occurrence);
                    }
                }
                break;
        }

        return occurrences;
    }
}
