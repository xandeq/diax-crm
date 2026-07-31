namespace Diax.Domain.Finance.Assets;

public interface IAssetRepository
{
    Task<List<Asset>> GetAllByUserIdAsync(
        Guid userId,
        AssetClass? assetClass = null,
        AssetOwnership? ownership = null,
        AssetLiquidity? liquidity = null,
        CancellationToken ct = default);

    Task<Asset?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Asset?> GetByIdAndUserWithValuationsAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task AddValuationAsync(AssetValuation valuation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default);
}
