using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class IncomeCategoryService : IApplicationService
{
    private readonly IIncomeCategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public IncomeCategoryService(IIncomeCategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<IncomeCategoryResponse>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetActiveAsync(cancellationToken);
        var response = categories.Select(c => new IncomeCategoryResponse(c.Id, c.Name, c.IsActive));
        return Result<IEnumerable<IncomeCategoryResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<IncomeCategoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);
        var response = categories.Select(c => new IncomeCategoryResponse(c.Id, c.Name, c.IsActive));
        return Result<IEnumerable<IncomeCategoryResponse>>.Success(response);
    }

    public async Task<Result<IncomeCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return Result.Failure<IncomeCategoryResponse>(new Error("Category.NotFound", "Category not found"));
        }
        return Result<IncomeCategoryResponse>.Success(new IncomeCategoryResponse(category.Id, category.Name, category.IsActive));
    }

    public async Task<Result<Guid>> CreateAsync(CreateIncomeCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new IncomeCategory(request.Name);
        await _repository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateIncomeCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return Result.Failure(new Error("Category.NotFound", "Category not found"));
        }

        category.Update(request.Name, request.IsActive);
        await _repository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
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
