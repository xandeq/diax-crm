using Diax.Application.Common;
using Diax.Application.Household.Dtos;
using Diax.Shared.Models;
using Diax.Shared.Results;

namespace Diax.Application.Household;

public interface IChecklistItemService : IApplicationService
{
    Task<Result<ChecklistItemDto>> GetByIdAsync(Guid id);
    Task<PagedResponse<ChecklistItemDto>> GetPagedAsync(ChecklistItemsQuery query);
    Task<Result<ChecklistItemDto>> CreateAsync(CreateChecklistItemRequest request);
    Task<Result<ChecklistItemDto>> UpdateAsync(Guid id, UpdateChecklistItemRequest request);
    Task<Result> DeleteAsync(Guid id);

    // Custom actions
    Task<Result> MarkBoughtAsync(Guid id, decimal? actualPrice);
    Task<Result> MarkCanceledAsync(Guid id);
    Task<Result> ReactivateAsync(Guid id);
    Task<Result> ArchiveAsync(Guid id);
    Task<Result> UnarchiveAsync(Guid id);

    // Bulk actions
    Task<Result<int>> ExecuteBulkActionAsync(ChecklistItemBulkRequest request);
    Task<Result<int>> ImportAsync(ImportChecklistRequest request);
}
