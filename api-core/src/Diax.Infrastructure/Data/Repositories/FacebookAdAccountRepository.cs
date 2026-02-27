using Diax.Domain.Ads;
using Diax.Domain.Ads.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class FacebookAdAccountRepository : IFacebookAdAccountRepository
{
    private readonly DiaxDbContext _context;

    public FacebookAdAccountRepository(DiaxDbContext context)
    {
        _context = context;
    }

    public async Task<FacebookAdAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.FacebookAdAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FacebookAdAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.FacebookAdAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(FacebookAdAccount account, CancellationToken ct = default)
    {
        await _context.FacebookAdAccounts.AddAsync(account, ct);
    }

    public void Update(FacebookAdAccount account)
    {
        _context.FacebookAdAccounts.Update(account);
    }

    public void Remove(FacebookAdAccount account)
    {
        _context.FacebookAdAccounts.Remove(account);
    }
}
