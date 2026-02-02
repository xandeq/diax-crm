using Diax.Application.Common;
using Diax.Application.Household.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Household;

public interface IChecklistCategoryService : IApplicationService
{
    Task<Result<ChecklistCategoryDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<ChecklistCategoryDto>>> GetAllAsync();
    Task<Result<ChecklistCategoryDto>> CreateAsync(CreateChecklistCategoryRequest request);
    Task<Result<ChecklistCategoryDto>> UpdateAsync(Guid id, UpdateChecklistCategoryRequest request);
    Task<Result> DeleteAsync(Guid id);
}
