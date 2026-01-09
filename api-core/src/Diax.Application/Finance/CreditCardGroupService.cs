using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;

namespace Diax.Application.Finance;

public class CreditCardGroupService
{
    private readonly ICreditCardGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardGroupService(ICreditCardGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CreditCardGroupResponse>> GetAllAsync()
    {
        var groups = await _repository.GetAllAsync();
        return groups.Select(MapToResponse);
    }

    public async Task<IEnumerable<CreditCardGroupResponse>> GetActiveGroupsAsync()
    {
        var groups = await _repository.GetActiveGroupsAsync();
        return groups.Select(MapToResponse);
    }

    public async Task<CreditCardGroupResponse?> GetByIdAsync(Guid id)
    {
        var group = await _repository.GetByIdWithCardsAsync(id);
        return group == null ? null : MapToResponse(group);
    }

    public async Task<CreditCardGroupResponse> CreateAsync(CreateCreditCardGroupRequest request)
    {
        var group = new CreditCardGroup(
            request.Name,
            request.Bank,
            request.ClosingDay,
            request.DueDay,
            request.SharedLimit,
            request.IsActive);

        await _repository.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(group);
    }

    public async Task<CreditCardGroupResponse?> UpdateAsync(Guid id, UpdateCreditCardGroupRequest request)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
            return null;

        group.Update(
            request.Name,
            request.Bank,
            request.ClosingDay,
            request.DueDay,
            request.SharedLimit,
            request.IsActive);

        await _repository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();

        // Reload with cards to calculate limits
        var updatedGroup = await _repository.GetByIdWithCardsAsync(id);
        return updatedGroup == null ? null : MapToResponse(updatedGroup);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
            return false;

        await _repository.DeleteAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static CreditCardGroupResponse MapToResponse(CreditCardGroup group)
    {
        return new CreditCardGroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Bank = group.Bank,
            ClosingDay = group.ClosingDay,
            DueDay = group.DueDay,
            SharedLimit = group.SharedLimit,
            IsActive = group.IsActive,
            TotalCardLimits = group.GetTotalCardLimits(),
            AvailableLimit = group.GetAvailableLimit(),
            CardCount = group.Cards?.Count ?? 0,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}
