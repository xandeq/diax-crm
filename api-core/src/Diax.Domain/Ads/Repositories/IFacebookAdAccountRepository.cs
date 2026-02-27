namespace Diax.Domain.Ads.Repositories;

public interface IFacebookAdAccountRepository
{
    Task<FacebookAdAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<FacebookAdAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(FacebookAdAccount account, CancellationToken ct = default);
    void Update(FacebookAdAccount account);
    void Remove(FacebookAdAccount account);
}
