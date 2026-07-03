using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public interface IProposalRepository : IRepository<Proposal>
{
    Task<Proposal?> GetByPublicTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<Proposal>> GetByUserAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default);
    Task<List<Proposal>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
