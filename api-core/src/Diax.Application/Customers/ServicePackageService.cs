using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Shared.Results;

namespace Diax.Application.Customers;

public record UpsertServicePackageRequest(
    string Name, string ShortDescription, string Scope, decimal Price, int DeliveryDays, int SortOrder, bool IsActive);

public record ServicePackageDto(
    Guid Id, string Name, string ShortDescription, string Scope, decimal Price, int DeliveryDays, int SortOrder, bool IsActive);

public class ServicePackageService : IApplicationService
{
    private readonly IServicePackageRepository _repo;
    private readonly IUnitOfWork _uow;

    public ServicePackageService(IServicePackageRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public static ServicePackageDto ToDto(ServicePackage p) =>
        new(p.Id, p.Name, p.ShortDescription, p.Scope, p.Price, p.DeliveryDays, p.SortOrder, p.IsActive);

    public async Task<List<ServicePackageDto>> ListAsync(Guid userId, bool onlyActive, CancellationToken ct = default)
        => (await _repo.GetByUserAsync(userId, onlyActive, ct)).Select(ToDto).ToList();

    public async Task<Result<ServicePackageDto>> CreateAsync(UpsertServicePackageRequest r, Guid userId, CancellationToken ct = default)
    {
        ServicePackage p;
        try { p = new ServicePackage(userId, r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder); }
        catch (ArgumentException ex) { return Result.Failure<ServicePackageDto>(Error.Validation("ServicePackage", ex.Message)); }
        if (!r.IsActive) p.Update(r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder, false);
        await _repo.AddAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(p));
    }

    public async Task<Result<ServicePackageDto>> UpdateAsync(Guid id, UpsertServicePackageRequest r, Guid userId, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p == null || p.UserId != userId)
            return Result.Failure<ServicePackageDto>(Error.NotFound("ServicePackage", id.ToString()));
        try { p.Update(r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder, r.IsActive); }
        catch (ArgumentException ex) { return Result.Failure<ServicePackageDto>(Error.Validation("ServicePackage", ex.Message)); }
        await _repo.UpdateAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(p));
    }
}
