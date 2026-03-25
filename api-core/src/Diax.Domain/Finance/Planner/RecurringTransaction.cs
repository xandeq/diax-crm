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
    /// Classificação do item recorrente.
    /// </summary>
    public RecurringItemKind ItemKind { get; set; } = RecurringItemKind.Standard;

    /// <summary>
    /// Descrição da transação recorrente
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detalhes adicionais do template.
    /// </summary>
    public string? Details { get; set; }

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

    public void Update(
        TransactionType type,
        string description,
        decimal amount,
        Guid categoryId,
        FrequencyType frequencyType,
        int dayOfMonth,
        DateTime startDate,
        DateTime? endDate,
        PaymentMethod paymentMethod,
        Guid? creditCardId,
        Guid? financialAccountId,
        bool isActive,
        int priority,
        string? details,
        RecurringItemKind itemKind)
    {
        Type = type;
        Description = description;
        Amount = amount;
        CategoryId = categoryId;
        FrequencyType = frequencyType;
        DayOfMonth = dayOfMonth;
        StartDate = startDate;
        EndDate = endDate;
        PaymentMethod = paymentMethod;
        CreditCardId = creditCardId;
        FinancialAccountId = financialAccountId;
        IsActive = isActive;
        Priority = priority;
        Details = details;
        ItemKind = itemKind;
    }

    /// <summary>
    /// Verifica se a recorrência se aplica a um mês/ano específico
    /// </summary>
    public bool IsApplicableForMonth(int month, int year)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        if (monthEnd < StartDate.Date)
            return false;

        if (EndDate.HasValue && monthStart > EndDate.Value.Date)
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
            case FrequencyType.Daily:
                {
                    var currentDate = startDate.Date < StartDate.Date ? StartDate.Date : startDate.Date;
                    while (currentDate <= endDate)
                    {
                        if (currentDate >= StartDate.Date && (!EndDate.HasValue || currentDate <= EndDate.Value.Date))
                        {
                            occurrences.Add(currentDate);
                        }

                        currentDate = currentDate.AddDays(1);
                    }
                    break;
                }

            case FrequencyType.Weekly:
                {
                    var currentDate = startDate.Date < StartDate.Date ? StartDate.Date : startDate.Date;
                    while (currentDate <= endDate)
                    {
                        if (currentDate >= StartDate.Date && (!EndDate.HasValue || currentDate <= EndDate.Value.Date))
                        {
                            occurrences.Add(currentDate);
                        }

                        currentDate = currentDate.AddDays(7);
                    }
                    break;
                }

            case FrequencyType.Monthly:
                {
                    var monthlyCursor = new DateTime(startDate.Year, startDate.Month, 1);
                    while (monthlyCursor <= endDate)
                    {
                        var daysInMonth = DateTime.DaysInMonth(monthlyCursor.Year, monthlyCursor.Month);
                        var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;
                        var occurrence = new DateTime(monthlyCursor.Year, monthlyCursor.Month, day);

                        if (occurrence >= StartDate.Date
                            && occurrence >= startDate
                            && occurrence <= endDate
                            && IsApplicableForMonth(monthlyCursor.Month, monthlyCursor.Year)
                            && (!EndDate.HasValue || occurrence <= EndDate.Value.Date))
                        {
                            occurrences.Add(occurrence);
                        }

                        monthlyCursor = monthlyCursor.AddMonths(1);
                    }
                    break;
                }

            case FrequencyType.Quarterly:
                {
                    var quarterlyCursor = new DateTime(startDate.Year, startDate.Month, 1);
                    while (quarterlyCursor <= endDate)
                    {
                        var daysInMonth = DateTime.DaysInMonth(quarterlyCursor.Year, quarterlyCursor.Month);
                        var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;
                        var occurrence = new DateTime(quarterlyCursor.Year, quarterlyCursor.Month, day);

                        if (occurrence >= StartDate.Date
                            && occurrence >= startDate
                            && occurrence <= endDate
                            && IsApplicableForMonth(quarterlyCursor.Month, quarterlyCursor.Year)
                            && (!EndDate.HasValue || occurrence <= EndDate.Value.Date))
                        {
                            occurrences.Add(occurrence);
                        }

                        quarterlyCursor = quarterlyCursor.AddMonths(3);
                    }
                    break;
                }

            case FrequencyType.Yearly:
                {
                    for (int year = startDate.Year; year <= endDate.Year; year++)
                    {
                        var month = StartDate.Month;
                        var daysInMonth = DateTime.DaysInMonth(year, month);
                        var day = DayOfMonth > daysInMonth ? daysInMonth : DayOfMonth;
                        var occurrence = new DateTime(year, month, day);

                        if (occurrence >= StartDate.Date
                            && occurrence >= startDate
                            && occurrence <= endDate
                            && IsApplicableForMonth(month, year)
                            && (!EndDate.HasValue || occurrence <= EndDate.Value.Date))
                        {
                            occurrences.Add(occurrence);
                        }
                    }
                    break;
                }
        }

        return occurrences;
    }
}
