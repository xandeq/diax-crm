using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class CreditCardService : IApplicationService
{
    private readonly ICreditCardRepository _repository;
    private readonly ICreditCardGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardService(
        ICreditCardRepository repository,
        ICreditCardGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<CreditCardResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var creditCards = await _repository.GetAllAsync(cancellationToken);
        var response = creditCards.Select(c => MapToResponse(c));
        return Result<IEnumerable<CreditCardResponse>>.Success(response);
    }

    public async Task<Result<CreditCardResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var creditCard = await _repository.GetByIdAsync(id, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure<CreditCardResponse>(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        string? groupName = null;
        if (creditCard.CreditCardGroupId.HasValue)
        {
            var group = await _groupRepository.GetByIdAsync(creditCard.CreditCardGroupId.Value);
            groupName = group?.Name;
        }

        return Result<CreditCardResponse>.Success(MapToResponse(creditCard, groupName));
    }

    public async Task<Result<Guid>> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        var creditCard = new CreditCard(
            request.Name,
            request.LastFourDigits,
            request.Limit,
            request.ClosingDay,
            request.DueDay,
            request.Brand,
            request.CardKind,
            request.IsActive,
            request.CreditCardGroupId
        );

        await _repository.AddAsync(creditCard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(creditCard.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        var creditCard = await _repository.GetByIdAsync(id, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        creditCard.Update(
            request.Name,
            request.LastFourDigits,
            request.Limit,
            request.ClosingDay,
            request.DueDay,
            request.Brand,
            request.CardKind,
            request.IsActive,
            request.CreditCardGroupId
        );

        await _repository.UpdateAsync(creditCard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var creditCard = await _repository.GetByIdAsync(id, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        await _repository.DeleteAsync(creditCard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static CreditCardResponse MapToResponse(CreditCard creditCard, string? groupName = null)
    {
        return new CreditCardResponse(
            creditCard.Id,
            creditCard.Name,
            creditCard.LastFourDigits,
            creditCard.ClosingDay,
            creditCard.DueDay,
            creditCard.Limit,
            creditCard.Brand,
            creditCard.CardKind,
            creditCard.IsActive,
            creditCard.CreditCardGroupId,
            groupName,
            creditCard.CreatedAt,
            creditCard.UpdatedAt
        );
    }
}
