namespace Diax.Domain.Agents;

/// <summary>
/// Repository contract for pending agent actions.
/// Implementations must NOT call SaveChanges — that is handled by IUnitOfWork at the service layer.
/// </summary>
public interface IAgentPendingActionRepository
{
    /// <summary>Adds a new pending action to the context (not yet persisted until UoW commits).</summary>
    Task AddAsync(AgentPendingAction action, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a pending action by its id and owner userId.
    /// Returns null if not found or if the action belongs to a different user.
    /// NOTE: Caller should check IsPending / IsExpired after retrieval.
    /// </summary>
    Task<AgentPendingAction?> GetPendingByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Marks the action as modified in the context (not yet persisted until UoW commits).</summary>
    Task UpdateAsync(AgentPendingAction action, CancellationToken ct = default);
}
