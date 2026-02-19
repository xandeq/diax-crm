using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class EmailCampaignRepository : Repository<EmailCampaign>, IEmailCampaignRepository
{
    public EmailCampaignRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<EmailCampaign> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(item => item.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task IncrementSentAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await DbSet.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return;
        }

        campaign.IncrementSent();
        DbSet.Update(campaign);
    }

    public async Task IncrementFailedAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await DbSet.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return;
        }

        campaign.IncrementFailed();
        DbSet.Update(campaign);
    }
}
