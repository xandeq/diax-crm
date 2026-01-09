using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class FinancialAccountService : IApplicationService
{
    private readonly IFinancialAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public FinancialAccountService(IFinancialAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetAllAsync(cancellationToken);
        var response = accounts.Select(MapToResponse);
        return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetActiveAccountsAsync(cancellationToken);
        var response = accounts.Select(MapToResponse);
        return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
    }

    public async Task<Result<FinancialAccountResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return Result.Failure<FinancialAccountResponse>(new Error("FinancialAccount.NotFound", "Financial account not found"));
        }
        return Result<FinancialAccountResponse>.Success(MapToResponse(account));
    }

    public async Task<Result<Guid>> CreateAsync(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new FinancialAccount(
            request.Name,
            request.AccountType,
            request.InitialBalance,
            request.IsActive
        );

        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(account.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
        }

        account.Update(
            request.Name,
            request.AccountType,
            request.IsActive
        );

        await _repository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
        }

        await _repository.DeleteAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static FinancialAccountResponse MapToResponse(FinancialAccount account)
    {
        return new FinancialAccountResponse(
            account.Id,
            account.Name,
            account.AccountType,
            account.InitialBalance,
            account.Balance,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt
        );
    }
}
