using Diax.Domain.Household;

namespace Diax.Application.Household.Dtos;

public record ChecklistItemDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ChecklistItemStatus Status { get; init; }
    public ChecklistItemPriority? Priority { get; init; }
    public DateTime? TargetDate { get; init; }
    public DateTime? BoughtAt { get; init; }
    public DateTime? CanceledAt { get; init; }
    public decimal? EstimatedPrice { get; init; }
    public decimal? ActualPrice { get; init; }
    public decimal? PaidAmount { get; init; }
    public decimal? Quantity { get; init; }
    public string? StoreOrLink { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateChecklistItemRequest(
    Guid CategoryId,
    string Title,
    string? Description = null,
    ChecklistItemPriority? Priority = null,
    DateTime? TargetDate = null,
    decimal? EstimatedPrice = null,
    decimal? PaidAmount = null,
    decimal? Quantity = null,
    string? StoreOrLink = null);

public record UpdateChecklistItemRequest(
    Guid CategoryId,
    string Title,
    string? Description = null,
    ChecklistItemPriority? Priority = null,
    ChecklistItemStatus? Status = null,
    DateTime? TargetDate = null,
    decimal? EstimatedPrice = null,
    decimal? ActualPrice = null,
    decimal? PaidAmount = null,
    decimal? Quantity = null,
    string? StoreOrLink = null);

public record ChecklistItemsQuery(
    Guid? CategoryId = null,
    ChecklistItemStatus? Status = null,
    ChecklistItemPriority? Priority = null,
    string? Q = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    bool IncludeArchived = false,
    int PageSize = 20,
    int Page = 1,
    string? SortBy = null,
    string? SortDir = "asc");

public record ChecklistItemBulkRequest(
    Guid[] Ids,
    string Action,
    decimal? ActualPrice = null,
    decimal? PaidAmount = null,
    Guid? TargetCategoryId = null,
    ChecklistItemStatus? TargetStatus = null);
