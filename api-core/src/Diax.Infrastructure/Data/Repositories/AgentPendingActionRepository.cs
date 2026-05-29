using Diax.Domain.Agents;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core repository for AgentPendingAction.
/// Does NOT call SaveChanges — commit is handled by IUnitOfWork at the service layer.
/// </summary>
public class AgentPendingActionRepository : IAgentPendingActionRepository
{
    private readonly DiaxDbContext _db;

    public AgentPendingActionRepository(DiaxDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AgentPendingAction action, CancellationToken ct = default)
    {
        await _db.Set<AgentPendingAction>().AddAsync(action, ct);
    }

    public async Task<AgentPendingAction?> GetPendingByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.Set<AgentPendingAction>()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
    }

    public Task UpdateAsync(AgentPendingAction action, CancellationToken ct = default)
    {
        _db.Set<AgentPendingAction>().Update(action);
        return Task.CompletedTask;
    }
}
