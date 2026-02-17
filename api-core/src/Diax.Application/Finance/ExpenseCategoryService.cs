using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

[Obsolete("Use TransactionCategoryService instead. This service will be removed in a future version.")]
public class ExpenseCategoryService : IApplicationService
{
    private readonly IExpenseCategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseCategoryService(IExpenseCategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<ExpenseCategoryResponse>>> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var activeCategories = categories.Where(c => c.IsActive);
        var response = activeCategories.Select(c => new ExpenseCategoryResponse(c.Id, c.Name, c.IsActive));
        return Result<IEnumerable<ExpenseCategoryResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<ExpenseCategoryResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var response = categories.Select(c => new ExpenseCategoryResponse(c.Id, c.Name, c.IsActive));
        return Result<IEnumerable<ExpenseCategoryResponse>>.Success(response);
    }

    public async Task<Result<ExpenseCategoryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<ExpenseCategoryResponse>(new Error("Category.NotFound", "Category not found"));
        }

        var response = new ExpenseCategoryResponse(category.Id, category.Name, category.IsActive);
        return Result<ExpenseCategoryResponse>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateExpenseCategoryRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = new ExpenseCategory(request.Name, userId);
            await _repository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(category.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("Category.ValidationError", ex.Message));
        }
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (category == null)
        {
            return Result.Failure(new Error("Category.NotFound", "Category not found"));
        }

        try
        {
            category.Update(request.Name, request.IsActive);
            await _repository.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("Category.ValidationError", ex.Message));
        }
    }

    public async Task<Result> DeactivateAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (category == null)
        {
            return Result.Failure(new Error("Category.NotFound", "Category not found"));
        }

        category.Deactivate();
        await _repository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
