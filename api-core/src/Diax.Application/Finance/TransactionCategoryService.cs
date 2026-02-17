using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

/// <summary>
/// Serviço de categorias unificadas de transação.
/// Substitui IncomeCategoryService + ExpenseCategoryService.
/// </summary>
public class TransactionCategoryService : IApplicationService
{
    private readonly ITransactionCategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionCategoryService(ITransactionCategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<TransactionCategoryResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var categories = await _repository.GetAllByUserIdAsync(userId, ct);
        return Result<IEnumerable<TransactionCategoryResponse>>.Success(categories.Select(MapToResponse));
    }

    public async Task<Result<IEnumerable<TransactionCategoryResponse>>> GetActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var categories = await _repository.GetActiveAsync(userId, ct);
        return Result<IEnumerable<TransactionCategoryResponse>>.Success(categories.Select(MapToResponse));
    }

    public async Task<Result<IEnumerable<TransactionCategoryResponse>>> GetByApplicableToAsync(
        CategoryApplicableTo applicableTo, Guid userId, CancellationToken ct = default)
    {
        var categories = await _repository.GetByApplicableToAsync(applicableTo, userId, ct);
        return Result<IEnumerable<TransactionCategoryResponse>>.Success(categories.Select(MapToResponse));
    }

    public async Task<Result<TransactionCategoryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (category == null)
            return Result.Failure<TransactionCategoryResponse>(new Error("TransactionCategory.NotFound", "Categoria não encontrada"));

        return Result<TransactionCategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<Guid>> CreateAsync(CreateTransactionCategoryRequest request, Guid userId, CancellationToken ct = default)
    {
        var category = new TransactionCategory(request.Name, userId, request.ApplicableTo, request.IsActive);
        await _repository.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateTransactionCategoryRequest request, Guid userId, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (category == null)
            return Result.Failure(new Error("TransactionCategory.NotFound", "Categoria não encontrada"));

        category.Update(request.Name, request.IsActive, request.ApplicableTo);
        await _repository.UpdateAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (category == null)
            return Result.Failure(new Error("TransactionCategory.NotFound", "Categoria não encontrada"));

        category.Deactivate();
        await _repository.UpdateAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static TransactionCategoryResponse MapToResponse(TransactionCategory c)
    {
        return new TransactionCategoryResponse(c.Id, c.Name, c.IsActive, c.ApplicableTo, c.CreatedAt, c.UpdatedAt);
    }
}
