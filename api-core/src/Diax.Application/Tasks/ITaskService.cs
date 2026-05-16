using Diax.Application.Tasks.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Tasks;

public interface ITaskService
{
    Task<Result<IEnumerable<TaskResponse>>> GetAllAsync(Guid userId, TasksQuery query, CancellationToken ct = default);
    Task<Result<TaskResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, Guid userId, CancellationToken ct = default);
    Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest request, Guid userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TaskResponse>> CompleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TaskResponse>> ReopenAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TaskResponse>> ArchiveAsync(Guid id, Guid userId, CancellationToken ct = default);
}
