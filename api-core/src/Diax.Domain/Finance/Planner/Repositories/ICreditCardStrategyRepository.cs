namespace Diax.Domain.Finance.Planner.Repositories;

/// <summary>
/// Repositório para estratégias de cartão de crédito
/// </summary>
public interface ICreditCardStrategyRepository
{
    Task<CreditCardStrategy?> GetByIdAsync(Guid id, Guid userId);
    Task<CreditCardStrategy?> GetByCreditCardIdAsync(Guid creditCardId, Guid userId);
    Task<List<CreditCardStrategy>> GetAllByUserIdAsync(Guid userId);
    Task<List<CreditCardStrategy>> GetRecommendedStrategiesAsync(Guid userId);
    Task<CreditCardStrategy> AddAsync(CreditCardStrategy strategy);
    Task UpdateAsync(CreditCardStrategy strategy);
    Task DeleteAsync(Guid id, Guid userId);
    Task<bool> ExistsAsync(Guid id, Guid userId);
}
