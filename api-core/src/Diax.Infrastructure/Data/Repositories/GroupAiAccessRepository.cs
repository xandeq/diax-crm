using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class GroupAiAccessRepository : IGroupAiAccessRepository
{
    private readonly DiaxDbContext _context;

    public GroupAiAccessRepository(DiaxDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetAllowedProviderIdsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupAiProviderAccesses
            .Where(x => x.GroupId == groupId)
            .Select(x => x.ProviderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetAllowedModelIdsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupAiModelAccesses
            .Where(x => x.GroupId == groupId)
            .Select(x => x.AiModelId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateProviderAccessAsync(Guid groupId, List<Guid> providerIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GroupAiProviderAccesses
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(x => !providerIds.Contains(x.ProviderId)).ToList();
        var toAddIds = providerIds.Where(id => !existing.Any(e => e.ProviderId == id)).ToList();

        if (toRemove.Any()) _context.GroupAiProviderAccesses.RemoveRange(toRemove);
        
        foreach (var id in toAddIds)
        {
            _context.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(groupId, id));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateModelAccessAsync(Guid groupId, List<Guid> modelIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GroupAiModelAccesses
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(x => !modelIds.Contains(x.AiModelId)).ToList();
        var toAddIds = modelIds.Where(id => !existing.Any(e => e.AiModelId == id)).ToList();

        if (toRemove.Any()) _context.GroupAiModelAccesses.RemoveRange(toRemove);
        
        foreach (var id in toAddIds)
        {
            _context.GroupAiModelAccesses.Add(new GroupAiModelAccess(groupId, id));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasProviderAccessAsync(Guid groupId, Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupAiProviderAccesses
            .AnyAsync(x => x.GroupId == groupId && x.ProviderId == providerId, cancellationToken);
    }

    public async Task<bool> HasModelAccessAsync(Guid groupId, Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupAiModelAccesses
            .AnyAsync(x => x.GroupId == groupId && x.AiModelId == modelId, cancellationToken);
    }
}
