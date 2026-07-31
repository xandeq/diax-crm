namespace Diax.Domain.Finance.Assets;

public interface IInvestmentOpportunityRepository
{
    /// <summary>
    /// Retorna as oportunidades geradas para o usuário no dia informado (dayUtc = data UTC, sem hora),
    /// ordenadas por EaseRank (nulos por último) e depois Score decrescente.
    /// </summary>
    Task<List<InvestmentOpportunity>> GetByGeneratedDateAsync(Guid userId, DateTime dayUtc, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<InvestmentOpportunity> opportunities, CancellationToken cancellationToken = default);
}
