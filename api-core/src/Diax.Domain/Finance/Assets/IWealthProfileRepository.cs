namespace Diax.Domain.Finance.Assets;

public interface IWealthProfileRepository
{
    Task<WealthProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(WealthProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(WealthProfile profile, CancellationToken cancellationToken = default);
}
