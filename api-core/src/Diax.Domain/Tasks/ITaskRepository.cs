using Diax.Domain.Common;

namespace Diax.Domain.Tasks;

/// <summary>Próxima tarefa aberta de um lead (a de vencimento mais próximo).</summary>
public record NextTaskInfo(string Title, DateTime? DueDate);

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetByStatusAsync(Guid userId, TaskItemStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskItem>> GetOverdueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// IDs de customers (dentre os informados) que JÁ têm task aberta
    /// (Todo/InProgress, não arquivada) — dedup dos follow-ups automáticos.
    /// </summary>
    /// <summary>Existe tarefa aberta (Todo/InProgress, não arquivada) do dono para o cliente, com exatamente este título? Ignora o filtro global por usuário (chamadas anônimas de integração não têm usuário corrente), por isso escopa por ownerUserId.</summary>
    Task<bool> HasOpenTaskWithTitleAsync(Guid ownerUserId, Guid customerId, string title, CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> GetCustomerIdsWithOpenTasksAsync(
        IEnumerable<Guid> customerIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Próxima ação (task aberta, não arquivada, do usuário atual) de cada customer informado:
    /// a de vencimento mais próximo (sem data vem por último). Customers sem task ficam fora do dicionário.
    /// </summary>
    Task<Dictionary<Guid, NextTaskInfo>> GetNextOpenTasksByCustomerAsync(
        IEnumerable<Guid> customerIds, CancellationToken cancellationToken = default);
}
