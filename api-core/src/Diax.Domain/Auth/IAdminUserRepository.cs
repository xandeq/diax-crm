using Diax.Domain.Auth.Enums;

namespace Diax.Domain.Auth;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CountByRoleAsync(UserRole role, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task AddAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task DeleteAsync(AdminUser user, CancellationToken cancellationToken = default);
}
