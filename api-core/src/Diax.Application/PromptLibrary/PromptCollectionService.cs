using Diax.Application.Common;
using Diax.Application.PromptLibrary.Dtos;
using Diax.Domain.Common;
using Diax.Domain.PromptLibrary;
using Diax.Shared.Results;

namespace Diax.Application.PromptLibrary;

public interface IPromptCollectionService
{
    Task<IReadOnlyList<PromptCollectionDto>> ListAsync(CancellationToken ct = default);
    Task<Result<PromptCollectionDto>> CreateAsync(SavePromptCollectionRequest request, Guid userId, CancellationToken ct = default);
    Task<Result<PromptCollectionDto>> UpdateAsync(Guid id, SavePromptCollectionRequest request, CancellationToken ct = default);
    Task<Result> ReorderAsync(ReorderCollectionsRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class PromptCollectionService : IApplicationService, IPromptCollectionService
{
    private const string ErrNotFound = "PromptCollection.NotFound";
    private const string ErrValidation = "PromptCollection.Validation";
    private const string ErrConflict = "PromptCollection.Conflict";

    private readonly IPromptCollectionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PromptCollectionService(IPromptCollectionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PromptCollectionDto>> ListAsync(CancellationToken ct = default)
        => (await _repository.GetAllWithCountsAsync(ct))
            .Select(r => ToDto(r.Collection, r.PromptCount))
            .ToList();

    public async Task<Result<PromptCollectionDto>> CreateAsync(SavePromptCollectionRequest request, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var all = await _repository.GetAllAsync(ct);
            if (all.Any(c => string.Equals(c.Name, request.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Result.Failure<PromptCollectionDto>(new Error(ErrConflict, "Já existe uma coleção com esse nome."));

            var collection = new PromptCollection(userId, request.Name, request.Icon, request.Color, all.Count());
            await _repository.AddAsync(collection, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Success(ToDto(collection, 0));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PromptCollectionDto>(new Error(ErrValidation, ex.Message));
        }
    }

    public async Task<Result<PromptCollectionDto>> UpdateAsync(Guid id, SavePromptCollectionRequest request, CancellationToken ct = default)
    {
        var collection = await _repository.GetByIdAsync(id, ct);
        if (collection is null) return Result.Failure<PromptCollectionDto>(new Error(ErrNotFound, "Coleção não encontrada."));
        try
        {
            var all = await _repository.GetAllAsync(ct);
            if (all.Any(c => c.Id != id && string.Equals(c.Name, request.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Result.Failure<PromptCollectionDto>(new Error(ErrConflict, "Já existe uma coleção com esse nome."));

            collection.Update(request.Name, request.Icon, request.Color);
            await _unitOfWork.SaveChangesAsync(ct);
            var count = (await _repository.GetAllWithCountsAsync(ct)).FirstOrDefault(r => r.Collection.Id == id).PromptCount;
            return Result.Success(ToDto(collection, count));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PromptCollectionDto>(new Error(ErrValidation, ex.Message));
        }
    }

    public async Task<Result> ReorderAsync(ReorderCollectionsRequest request, CancellationToken ct = default)
    {
        var all = (await _repository.GetAllAsync(ct)).ToDictionary(c => c.Id);
        var order = 0;
        foreach (var id in request.Ids.Distinct())
            if (all.TryGetValue(id, out var c)) c.SetSortOrder(order++);
        foreach (var c in all.Values.Where(c => !request.Ids.Contains(c.Id)).OrderBy(c => c.SortOrder))
            c.SetSortOrder(order++);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await _repository.GetByIdAsync(id, ct);
        if (collection is null) return Result.Failure(new Error(ErrNotFound, "Coleção não encontrada."));
        // FK ON DELETE SET NULL: os prompts continuam existindo, sem coleção.
        await _repository.DeleteAsync(collection, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static PromptCollectionDto ToDto(PromptCollection c, int count)
        => new(c.Id, c.Name, c.Icon, c.Color, c.SortOrder, count);
}
