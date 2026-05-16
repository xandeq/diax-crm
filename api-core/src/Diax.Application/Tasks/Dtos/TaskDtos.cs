using Diax.Domain.Tasks;

namespace Diax.Application.Tasks.Dtos;

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskItemPriority Priority,
    DateTime? DueDate);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemPriority Priority,
    TaskItemStatus Status,
    DateTime? DueDate);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDate,
    DateTime? CompletedAt,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record TasksQuery(
    TaskItemStatus? Status = null,
    TaskItemPriority? Priority = null,
    bool IncludeArchived = false,
    bool OverdueOnly = false);
