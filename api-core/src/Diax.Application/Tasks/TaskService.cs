using Diax.Application.Tasks.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Tasks;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Tasks;

public class TaskService(
    ITaskRepository taskRepo,
    IUnitOfWork unitOfWork,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<Result<IEnumerable<TaskResponse>>> GetAllAsync(Guid userId, TasksQuery query, CancellationToken ct = default)
    {
        IEnumerable<TaskItem> tasks;

        if (query.OverdueOnly)
            tasks = await taskRepo.GetOverdueAsync(userId, ct);
        else if (query.Status.HasValue)
            tasks = await taskRepo.GetByStatusAsync(userId, query.Status.Value, ct);
        else
            tasks = await taskRepo.GetByUserAsync(userId, query.IncludeArchived, ct);

        if (query.Priority.HasValue)
            tasks = tasks.Where(t => t.Priority == query.Priority.Value);

        return Result.Success(tasks.Select(MapToResponse));
    }

    public async Task<Result<TaskResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure<TaskResponse>(new Error("Task.NotFound", "Tarefa não encontrada."));

        return Result.Success(MapToResponse(task));
    }

    public async Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<TaskResponse>(new Error("Task.TitleRequired", "O título é obrigatório."));

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate,
            UserId = userId
        };

        await taskRepo.AddAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Task {TaskId} created for user {UserId}", task.Id, userId);
        return Result.Success(MapToResponse(task));
    }

    public async Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest request, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure<TaskResponse>(new Error("Task.NotFound", "Tarefa não encontrada."));

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<TaskResponse>(new Error("Task.TitleRequired", "O título é obrigatório."));

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.DueDate = request.DueDate;

        if (request.Status == Domain.Tasks.TaskItemStatus.Done && task.CompletedAt is null)
            task.CompletedAt = DateTime.UtcNow;
        else if (request.Status != Domain.Tasks.TaskItemStatus.Done)
            task.CompletedAt = null;

        await taskRepo.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(task));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure(new Error("Task.NotFound", "Tarefa não encontrada."));

        await taskRepo.DeleteAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Task {TaskId} deleted for user {UserId}", id, userId);
        return Result.Success();
    }

    public async Task<Result<TaskResponse>> CompleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure<TaskResponse>(new Error("Task.NotFound", "Tarefa não encontrada."));

        task.Complete();
        await taskRepo.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(task));
    }

    public async Task<Result<TaskResponse>> ReopenAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure<TaskResponse>(new Error("Task.NotFound", "Tarefa não encontrada."));

        task.Reopen();
        await taskRepo.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(task));
    }

    public async Task<Result<TaskResponse>> ArchiveAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAndUserAsync(id, userId, ct);
        if (task is null)
            return Result.Failure<TaskResponse>(new Error("Task.NotFound", "Tarefa não encontrada."));

        task.Archive();
        await taskRepo.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(task));
    }

    private static TaskResponse MapToResponse(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.DueDate, t.CompletedAt, t.IsArchived, t.CreatedAt, t.UpdatedAt);
}
