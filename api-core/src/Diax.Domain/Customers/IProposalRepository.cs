using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public interface IProposalRepository : IRepository<Proposal>
{
    Task<Proposal?> GetByPublicTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<Proposal>> GetByUserAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default);
    Task<List<Proposal>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Resumo por status: contagem e soma (GroupBy no servidor) — dashboard comercial.</summary>
    Task<List<(ProposalStatus Status, int Count, decimal Total)>> GetStatusSummaryAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Propostas pagas desde a data — receita mensal do dashboard.</summary>
    Task<List<Proposal>> GetPaidSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
