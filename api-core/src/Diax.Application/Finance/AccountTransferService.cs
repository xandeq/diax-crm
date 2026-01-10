using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class AccountTransferService : IApplicationService
{
    private readonly IAccountTransferRepository _repository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountTransferService(
        IAccountTransferRepository repository,
        IFinancialAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transfers = await _repository.GetAllAsync(cancellationToken);
        var response = transfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<AccountTransferResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure<AccountTransferResponse>(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }
        return Result<AccountTransferResponse>.Success(MapToResponse(transfer));
    }

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var transfers = await _repository.GetByAccountIdAsync(accountId);
        var response = transfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var transfers = await _repository.GetByDateRangeAsync(startDate, endDate);
        var response = transfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateAccountTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Validate source account exists and is active
        var fromAccount = await _accountRepository.GetByIdAsync(request.FromFinancialAccountId, cancellationToken);
        if (fromAccount == null)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InvalidFromAccount", "Source account not found"));
        }

        if (!fromAccount.IsActive)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InactiveFromAccount", "Source account is inactive"));
        }

        // Validate destination account exists and is active
        var toAccount = await _accountRepository.GetByIdAsync(request.ToFinancialAccountId, cancellationToken);
        if (toAccount == null)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InvalidToAccount", "Destination account not found"));
        }

        if (!toAccount.IsActive)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InactiveToAccount", "Destination account is inactive"));
        }

        // Create transfer entity (validates accounts are different)
        var transfer = new AccountTransfer(
            request.FromFinancialAccountId,
            request.ToFinancialAccountId,
            request.Amount,
            request.Date,
            request.Description
        );

        // Debit source account
        fromAccount.Debit(request.Amount);

        // Credit destination account
        toAccount.Credit(request.Amount);

        // Save all changes in single transaction
        await _repository.AddAsync(transfer, cancellationToken);
        await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
        await _accountRepository.UpdateAsync(toAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(transfer.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateAccountTransferRequest request, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }

        // Validate new source account
        var newFromAccount = await _accountRepository.GetByIdAsync(request.FromFinancialAccountId, cancellationToken);
        if (newFromAccount == null)
        {
            return Result.Failure(new Error("AccountTransfer.InvalidFromAccount", "Source account not found"));
        }

        if (!newFromAccount.IsActive)
        {
            return Result.Failure(new Error("AccountTransfer.InactiveFromAccount", "Source account is inactive"));
        }

        // Validate new destination account
        var newToAccount = await _accountRepository.GetByIdAsync(request.ToFinancialAccountId, cancellationToken);
        if (newToAccount == null)
        {
            return Result.Failure(new Error("AccountTransfer.InvalidToAccount", "Destination account not found"));
        }

        if (!newToAccount.IsActive)
        {
            return Result.Failure(new Error("AccountTransfer.InactiveToAccount", "Destination account is inactive"));
        }

        // Reverse old transfer
        var oldFromAccount = await _accountRepository.GetByIdAsync(transfer.FromFinancialAccountId, cancellationToken);
        if (oldFromAccount != null)
        {
            oldFromAccount.Credit(transfer.Amount); // Reverse debit
            await _accountRepository.UpdateAsync(oldFromAccount, cancellationToken);
        }

        var oldToAccount = await _accountRepository.GetByIdAsync(transfer.ToFinancialAccountId, cancellationToken);
        if (oldToAccount != null)
        {
            oldToAccount.Debit(transfer.Amount); // Reverse credit
            await _accountRepository.UpdateAsync(oldToAccount, cancellationToken);
        }

        // Apply new transfer
        newFromAccount.Debit(request.Amount);
        newToAccount.Credit(request.Amount);

        transfer.Update(
            request.FromFinancialAccountId,
            request.ToFinancialAccountId,
            request.Amount,
            request.Date,
            request.Description
        );

        await _repository.UpdateAsync(transfer, cancellationToken);
        await _accountRepository.UpdateAsync(newFromAccount, cancellationToken);
        await _accountRepository.UpdateAsync(newToAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }

        // Reverse transfer: credit source, debit destination
        var fromAccount = await _accountRepository.GetByIdAsync(transfer.FromFinancialAccountId, cancellationToken);
        if (fromAccount != null)
        {
            fromAccount.Credit(transfer.Amount);
            await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
        }

        var toAccount = await _accountRepository.GetByIdAsync(transfer.ToFinancialAccountId, cancellationToken);
        if (toAccount != null)
        {
            toAccount.Debit(transfer.Amount);
            await _accountRepository.UpdateAsync(toAccount, cancellationToken);
        }

        await _repository.DeleteAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static AccountTransferResponse MapToResponse(AccountTransfer transfer)
    {
        return new AccountTransferResponse(
            transfer.Id,
            transfer.FromFinancialAccountId,
            transfer.FromFinancialAccount?.Name ?? "Unknown",
            transfer.ToFinancialAccountId,
            transfer.ToFinancialAccount?.Name ?? "Unknown",
            transfer.Amount,
            transfer.Date,
            transfer.Description,
            transfer.CreatedAt,
            transfer.UpdatedAt
        );
    }
}
