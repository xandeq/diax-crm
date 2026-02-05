using Diax.Domain.Auth;
using Diax.Domain.Auth.Enums;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly DiaxDbContext _context;

    public AdminUserRepository(DiaxDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByRoleAsync(UserRole role, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.AdminUsers.AsQueryable();
        
        if (onlyActive)
            query = query.Where(x => x.IsActive);
            
        return await query.CountAsync(x => x.Role == role, cancellationToken);
    }

    public async Task AddAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        await _context.AdminUsers.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
