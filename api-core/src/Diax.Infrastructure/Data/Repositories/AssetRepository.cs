using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<List<Asset>> GetAllByUserIdAsync(
        Guid userId,
        AssetClass? assetClass = null,
        AssetOwnership? ownership = null,
        AssetLiquidity? liquidity = null,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(x => x.UserId == userId);

        if (assetClass.HasValue)
        {
            query = query.Where(x => x.Class == assetClass.Value);
        }

        if (ownership.HasValue)
        {
            query = query.Where(x => x.Ownership == ownership.Value);
        }

        if (liquidity.HasValue)
        {
            query = query.Where(x => x.Liquidity == liquidity.Value);
        }

        return await query
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<Asset?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }

    public async Task<Asset?> GetByIdAndUserWithValuationsAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(x => x.Valuations)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }

    public new async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(asset, cancellationToken);
    }

    public async Task AddValuationAsync(AssetValuation valuation, CancellationToken cancellationToken = default)
    {
        await Context.AssetValuations.AddAsync(valuation, cancellationToken);
    }

    public new Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        DbSet.Update(asset);
        return Task.CompletedTask;
    }

    public new Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(asset);
        return Task.CompletedTask;
    }
}
