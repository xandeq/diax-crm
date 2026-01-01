using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class CreditCardService : IApplicationService
{
    private readonly ICreditCardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardService(ICreditCardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<CreditCardResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var creditCards = await _repository.GetAllAsync(cancellationToken);
        var response = creditCards.Select(MapToResponse);
        return Result<IEnumerable<CreditCardResponse>>.Success(response);
    }

    public async Task<Result<CreditCardResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var creditCard = await _repository.GetByIdAsync(id, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure<CreditCardResponse>(new Error("CreditCard.NotFound", "Credit card not found"));
        }
        return Result<CreditCardResponse>.Success(MapToResponse(creditCard));
    }

    public async Task<Result<Guid>> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken = default)
    {
        var creditCard = new CreditCard(
            request.Name,
            request.LastFourDigits,
            request.Limit,
            request.ClosingDay,
            request.DueDay
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
            request.DueDay
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

    private static CreditCardResponse MapToResponse(CreditCard creditCard)
    {
        return new CreditCardResponse(
            creditCard.Id,
            creditCard.Name,
            creditCard.LastFourDigits,
            creditCard.ClosingDay,
            creditCard.DueDay,
            creditCard.Limit,
            creditCard.CreatedAt,
            creditCard.UpdatedAt
        );
    }
}
