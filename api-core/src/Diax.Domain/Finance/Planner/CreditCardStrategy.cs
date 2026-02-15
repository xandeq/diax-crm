using Diax.Domain.Common;

namespace Diax.Domain.Finance.Planner;

/// <summary>
/// Estratégia otimizada de uso de cartão de crédito
/// </summary>
public class CreditCardStrategy : AuditableEntity, IUserOwnedEntity
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ID do cartão de crédito
    /// </summary>
    public Guid CreditCardId { get; set; }

    /// <summary>
    /// Melhor dia do mês para fazer compras (ciclo máximo 30-40 dias)
    /// </summary>
    public int OptimalPurchaseDay { get; set; }

    /// <summary>
    /// Duração máxima do ciclo (dias até vencimento)
    /// </summary>
    public int MaximumCycleLength { get; set; }

    /// <summary>
    /// Percentual de limite disponível
    /// </summary>
    public decimal AvailableLimitPercentage { get; set; }

    /// <summary>
    /// Data da última atualização do cálculo
    /// </summary>
    public DateTime LastCalculated { get; set; }

    /// <summary>
    /// Se este cartão é recomendado para novas compras
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Dia de fechamento da fatura
    /// </summary>
    public int ClosingDay { get; set; }

    /// <summary>
    /// Dia de vencimento da fatura
    /// </summary>
    public int DueDay { get; set; }

    /// <summary>
    /// Calcula o dia ótimo para compra (logo após o fechamento)
    /// </summary>
    public void CalculateOptimalDay()
    {
        // Comprar logo após o fechamento maximiza o ciclo
        // Se fechamento = dia 5, melhor comprar dia 6-7
        // Se fechamento = dia 31, melhor comprar dia 1
        OptimalPurchaseDay = ClosingDay == 31 ? 1 : ClosingDay + 1;

        // Calcular duração máxima do ciclo
        if (DueDay > ClosingDay)
        {
            // Exemplo: Fecha dia 5, vence dia 15 → 10 dias + ~30 dias = 40 dias
            MaximumCycleLength = (DueDay - ClosingDay) + 30;
        }
        else
        {
            // Vencimento no mês seguinte
            MaximumCycleLength = (30 - ClosingDay) + DueDay + 30;
        }

        LastCalculated = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula quantos dias faltam para o pagamento de uma compra feita hoje
    /// </summary>
    public int GetDaysUntilPayment(DateTime purchaseDate)
    {
        var purchaseDay = purchaseDate.Day;

        // Se comprou APÓS o fechamento do mês atual → vai para próxima fatura
        if (purchaseDay > ClosingDay)
        {
            // Próximo vencimento
            var nextMonth = purchaseDate.AddMonths(1);
            var nextDueDate = new DateTime(nextMonth.Year, nextMonth.Month, DueDay);
            return (nextDueDate - purchaseDate).Days;
        }
        else
        {
            // Vai para a fatura atual
            var currentDueDate = new DateTime(purchaseDate.Year, purchaseDate.Month, DueDay);

            // Se o vencimento já passou, vai para o próximo mês
            if (currentDueDate < purchaseDate)
            {
                currentDueDate = currentDueDate.AddMonths(1);
            }

            return (currentDueDate - purchaseDate).Days;
        }
    }

    /// <summary>
    /// Verifica se o cartão tem limite disponível suficiente
    /// </summary>
    public bool HasSufficientLimit(decimal purchaseAmount, decimal currentLimit)
    {
        if (currentLimit == 0)
            return false;

        decimal availableLimit = currentLimit * (AvailableLimitPercentage / 100m);
        return availableLimit >= purchaseAmount;
    }

    /// <summary>
    /// Atualiza o percentual de limite disponível
    /// </summary>
    public void UpdateAvailableLimitPercentage(decimal usedLimit, decimal totalLimit)
    {
        if (totalLimit == 0)
        {
            AvailableLimitPercentage = 0;
            return;
        }

        decimal availableLimit = totalLimit - usedLimit;
        AvailableLimitPercentage = Math.Round((availableLimit / totalLimit) * 100, 2);
    }
}
