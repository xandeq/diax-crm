using Diax.Application.Auth;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Auth;

/// <summary>
/// Implementação do IPermissionService usando DiaxDbContext.
/// Verifica permissões e grupos dos usuários via RBAC.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly DiaxDbContext _context;

    public PermissionService(DiaxDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserGroupMembers
            .AnyAsync(ugm => ugm.UserId == userId && ugm.Group.Key == "system-admin", ct);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct = default)
    {
        // Admin tem acesso total
        if (await IsAdminAsync(userId, ct)) return true;

        var userGroupIds = await _context.UserGroupMembers
            .Where(ugm => ugm.UserId == userId)
            .Select(ugm => ugm.GroupId)
            .ToListAsync(ct);

        if (!userGroupIds.Any()) return false;

        return await _context.GroupPermissions
            .AnyAsync(gp => userGroupIds.Contains(gp.GroupId) && gp.Permission.Key == permissionKey, ct);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var userGroupIds = await _context.UserGroupMembers
            .Where(ugm => ugm.UserId == userId)
            .Select(ugm => ugm.GroupId)
            .ToListAsync(ct);

        if (!userGroupIds.Any()) return Array.Empty<string>();

        var permissions = await _context.GroupPermissions
            .Where(gp => userGroupIds.Contains(gp.GroupId))
            .Select(gp => gp.Permission.Key)
            .Distinct()
            .ToListAsync(ct);

        return permissions.AsReadOnly();
    }

    public async Task<IReadOnlyList<string>> GetGroupKeysAsync(Guid userId, CancellationToken ct = default)
    {
        var groups = await _context.UserGroupMembers
            .Where(ugm => ugm.UserId == userId)
            .Select(ugm => ugm.Group.Key)
            .ToListAsync(ct);

        return groups.AsReadOnly();
    }

    public async Task<int> CountUsersInGroupAsync(string groupKey, bool onlyActive = true, CancellationToken ct = default)
    {
        var query = from ugm in _context.UserGroupMembers
                    join u in _context.Users on ugm.UserId equals u.Id
                    where ugm.Group.Key == groupKey
                    select u;

        if (onlyActive)
            query = query.Where(u => u.IsActive);

        return await query.CountAsync(ct);
    }
}
