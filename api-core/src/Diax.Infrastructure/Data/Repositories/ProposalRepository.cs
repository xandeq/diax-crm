using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ProposalRepository : Repository<Proposal>, IProposalRepository
{
    public ProposalRepository(DiaxDbContext context) : base(context) { }

    public async Task<Proposal?> GetByPublicTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.PublicToken == token, cancellationToken);
    }

    public async Task<List<Proposal>> GetByUserAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Proposal>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(ProposalStatus Status, int Count, decimal Total)>> GetStatusSummaryAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await DbSet
            .Where(p => p.UserId == userId)
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);
        return rows.Select(r => (r.Key, r.Count, r.Total)).ToList();
    }

    public async Task<List<Proposal>> GetPaidSinceAsync(
        Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.UserId == userId
                && p.Status == ProposalStatus.Paid
                && p.PaidAt != null
                && p.PaidAt >= sinceUtc)
            .ToListAsync(cancellationToken);
    }
}
