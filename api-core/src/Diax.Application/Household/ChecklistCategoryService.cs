using Diax.Application.Household.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Household;
using Diax.Shared.Results;

namespace Diax.Application.Household;

public class ChecklistCategoryService : IChecklistCategoryService
{
    private readonly IChecklistCategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChecklistCategoryService(IChecklistCategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChecklistCategoryDto>> GetByIdAsync(Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null) return Result.Failure<ChecklistCategoryDto>(new Error("ChecklistCategory.NotFound", "Categoria não encontrada."));

        return Result.Success(MapToDto(category));
    }

    public async Task<Result<IEnumerable<ChecklistCategoryDto>>> GetAllAsync()
    {
        var categories = await _repository.GetAllAsync();
        return Result.Success(categories.Select(MapToDto));
    }

    public async Task<Result<ChecklistCategoryDto>> CreateAsync(CreateChecklistCategoryRequest request)
    {
        var category = new ChecklistCategory
        {
            Name = request.Name,
            Color = request.Color,
            SortOrder = request.SortOrder
        };

        await _repository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(category));
    }

    public async Task<Result<ChecklistCategoryDto>> UpdateAsync(Guid id, UpdateChecklistCategoryRequest request)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null) return Result.Failure<ChecklistCategoryDto>(new Error("ChecklistCategory.NotFound", "Categoria não encontrada."));

        category.Name = request.Name;
        category.Color = request.Color;
        category.SortOrder = request.SortOrder;

        await _repository.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(category));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null) return Result.Failure(new Error("ChecklistCategory.NotFound", "Categoria não encontrada."));

        if (category.Items != null && category.Items.Any())
            return Result.Failure(new Error("ChecklistCategory.Conflict", "Não é possível excluir uma categoria que possui itens vinculados."));

        await _repository.DeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    private static ChecklistCategoryDto MapToDto(ChecklistCategory category)
    {
        return new ChecklistCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
