using System.Linq.Expressions;
using Diax.Application.Common;
using Diax.Application.Household.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Household;
using Diax.Shared.Results;

namespace Diax.Application.Household;

public class ChecklistItemService : IChecklistItemService
{
    private readonly IChecklistItemRepository _repository;
    private readonly IChecklistCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChecklistItemService(
        IChecklistItemRepository repository,
        IChecklistCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChecklistItemDto>> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure<ChecklistItemDto>(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        return Result.Success(MapToDto(item));
    }

    public async Task<PagedResponse<ChecklistItemDto>> GetPagedAsync(ChecklistItemsQuery query)
    {
        Expression<Func<ChecklistItem, bool>> predicate = i =>
            (!query.CategoryId.HasValue || i.CategoryId == query.CategoryId.Value) &&
            (!query.Status.HasValue || i.Status == query.Status.Value) &&
            (string.IsNullOrWhiteSpace(query.Q) || i.Title.Contains(query.Q) || (i.Description != null && i.Description.Contains(query.Q))) &&
            (!query.DateFrom.HasValue || i.TargetDate >= query.DateFrom.Value) &&
            (!query.DateTo.HasValue || i.TargetDate <= query.DateTo.Value) &&
            (query.IncludeArchived || (!i.IsArchived && i.Status != ChecklistItemStatus.Archived));

        var isDesc = query.SortDir?.ToLower() == "desc";

        Func<IQueryable<ChecklistItem>, IOrderedQueryable<ChecklistItem>> orderBy = query.SortBy?.ToLower() switch
        {
            "title" => q => isDesc ? q.OrderByDescending(i => i.Title) : q.OrderBy(i => i.Title),
            "status" => q => isDesc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
            "targetdate" => q => isDesc ? q.OrderByDescending(i => i.TargetDate) : q.OrderBy(i => i.TargetDate),
            "estimatedprice" => q => isDesc ? q.OrderByDescending(i => i.EstimatedPrice) : q.OrderBy(i => i.EstimatedPrice),
            "actualprice" => q => isDesc ? q.OrderByDescending(i => i.ActualPrice) : q.OrderBy(i => i.ActualPrice),
            "categoryname" => q => isDesc ? q.OrderByDescending(i => i.Category.Name) : q.OrderBy(i => i.Category.Name),
            "updatedat" => q => isDesc ? q.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt) : q.OrderBy(i => i.UpdatedAt ?? i.CreatedAt),
            _ => q => q.OrderByDescending(i => i.CreatedAt)
        };

        var pagedResult = await _repository.GetPagedAsync(
            query.Page,
            query.PageSize,
            predicate,
            orderBy);

        return PagedResponse<ChecklistItemDto>.Create(
            pagedResult.Items.Select(MapToDto),
            query.Page,
            query.PageSize,
            pagedResult.TotalCount);
    }

    public async Task<Result<ChecklistItemDto>> CreateAsync(CreateChecklistItemRequest request)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists) return Result.Failure<ChecklistItemDto>(new Error("ChecklistCategory.NotFound", "Categoria não encontrada."));

        var item = new ChecklistItem
        {
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            TargetDate = request.TargetDate,
            EstimatedPrice = request.EstimatedPrice,
            Quantity = request.Quantity,
            StoreOrLink = request.StoreOrLink,
            Status = ChecklistItemStatus.ToBuy
        };

        await _repository.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(item.Id);
    }

    public async Task<Result<ChecklistItemDto>> UpdateAsync(Guid id, UpdateChecklistItemRequest request)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure<ChecklistItemDto>(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists) return Result.Failure<ChecklistItemDto>(new Error("ChecklistCategory.NotFound", "Categoria não encontrada."));

        item.CategoryId = request.CategoryId;
        item.Title = request.Title;
        item.Description = request.Description;
        item.Priority = request.Priority;
        item.TargetDate = request.TargetDate;
        item.EstimatedPrice = request.EstimatedPrice;
        item.ActualPrice = request.ActualPrice;
        item.Quantity = request.Quantity;
        item.StoreOrLink = request.StoreOrLink;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(item.Id);
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        await _repository.DeleteAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> MarkBoughtAsync(Guid id, decimal? actualPrice)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        item.Status = ChecklistItemStatus.Bought;
        item.BoughtAt = DateTime.UtcNow;
        item.CanceledAt = null;
        if (actualPrice.HasValue) item.ActualPrice = actualPrice;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkCanceledAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        item.Status = ChecklistItemStatus.Canceled;
        item.CanceledAt = DateTime.UtcNow;
        item.BoughtAt = null;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReactivateAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        item.Status = ChecklistItemStatus.ToBuy;
        item.BoughtAt = null;
        item.CanceledAt = null;
        item.IsArchived = false;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        item.IsArchived = true;
        item.Status = ChecklistItemStatus.Archived;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UnarchiveAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null) return Result.Failure(new Error("ChecklistItem.NotFound", "Item não encontrado."));

        item.IsArchived = false;
        item.Status = ChecklistItemStatus.ToBuy;

        await _repository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<int>> ExecuteBulkActionAsync(ChecklistItemBulkRequest request)
    {
        if (request.Ids == null || !request.Ids.Any())
            return Result.Failure<int>(new Error("ChecklistItem.Bulk.NoIds", "Nenhum item selecionado."));

        var items = await _repository.FindAsync(i => request.Ids.Contains(i.Id));
        int count = 0;

        foreach (var item in items)
        {
            switch (request.Action.ToLower())
            {
                case "markbought":
                    item.Status = ChecklistItemStatus.Bought;
                    item.BoughtAt = DateTime.UtcNow;
                    item.CanceledAt = null;
                    if (request.ActualPrice.HasValue) item.ActualPrice = request.ActualPrice;
                    await _repository.UpdateAsync(item);
                    break;
                case "markcanceled":
                    item.Status = ChecklistItemStatus.Canceled;
                    item.CanceledAt = DateTime.UtcNow;
                    item.BoughtAt = null;
                    await _repository.UpdateAsync(item);
                    break;
                case "archive":
                    item.IsArchived = true;
                    item.Status = ChecklistItemStatus.Archived;
                    await _repository.UpdateAsync(item);
                    break;
                case "unarchive":
                    item.IsArchived = false;
                    item.Status = ChecklistItemStatus.ToBuy;
                    await _repository.UpdateAsync(item);
                    break;
                case "reactivate":
                    item.Status = ChecklistItemStatus.ToBuy;
                    item.BoughtAt = null;
                    item.CanceledAt = null;
                    item.IsArchived = false;
                    await _repository.UpdateAsync(item);
                    break;
                case "delete":
                    await _repository.DeleteAsync(item);
                    break;
            }
            count++;
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success(count);
    }

    private static ChecklistItemDto MapToDto(ChecklistItem item)
    {
        return new ChecklistItemDto
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            Title = item.Title,
            Description = item.Description,
            Status = item.Status,
            Priority = item.Priority,
            TargetDate = item.TargetDate,
            BoughtAt = item.BoughtAt,
            CanceledAt = item.CanceledAt,
            EstimatedPrice = item.EstimatedPrice,
            ActualPrice = item.ActualPrice,
            Quantity = item.Quantity,
            StoreOrLink = item.StoreOrLink,
            IsArchived = item.IsArchived,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
