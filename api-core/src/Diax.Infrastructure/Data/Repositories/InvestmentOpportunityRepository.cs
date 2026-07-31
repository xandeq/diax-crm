using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class InvestmentOpportunityRepository : Repository<InvestmentOpportunity>, IInvestmentOpportunityRepository
{
    public InvestmentOpportunityRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<List<InvestmentOpportunity>> GetByGeneratedDateAsync(
        Guid userId,
        DateTime dayUtc,
        CancellationToken ct = default)
    {
        var dayStart = dayUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        return await DbSet
            .Where(x => x.UserId == userId && x.GeneratedAt >= dayStart && x.GeneratedAt < dayEnd)
            .OrderBy(x => x.EaseRank ?? int.MaxValue)
            .ThenByDescending(x => x.Score)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<InvestmentOpportunity> opportunities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(opportunities, cancellationToken);
    }
}
