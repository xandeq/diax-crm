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

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var transfers = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var response = transfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<AccountTransferResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure<AccountTransferResponse>(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }
        return Result<AccountTransferResponse>.Success(MapToResponse(transfer));
    }

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetByAccountIdAsync(Guid accountId, Guid userId, CancellationToken cancellationToken = default)
    {
        // First verify account belongs to user
        var account = await _accountRepository.GetByIdAndUserAsync(accountId, userId, cancellationToken);
        if (account == null)
        {
            return Result.Failure<IEnumerable<AccountTransferResponse>>(new Error("FinancialAccount.NotFound", "Account not found"));
        }

        var transfers = await _repository.GetByAccountIdAsync(accountId);
        var response = transfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<AccountTransferResponse>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfers = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var filteredTransfers = transfers.Where(t => t.Date >= startDate && t.Date <= endDate);
        var response = filteredTransfers.Select(MapToResponse);
        return Result<IEnumerable<AccountTransferResponse>>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateAccountTransferRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        // Validate source account exists and is active
        var fromAccount = await _accountRepository.GetByIdAndUserAsync(request.FromFinancialAccountId, userId, cancellationToken);
        if (fromAccount == null)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InvalidFromAccount", "Source account not found"));
        }

        if (!fromAccount.IsActive)
        {
            return Result.Failure<Guid>(new Error("AccountTransfer.InactiveFromAccount", "Source account is inactive"));
        }

        // Validate destination account exists and is active
        var toAccount = await _accountRepository.GetByIdAndUserAsync(request.ToFinancialAccountId, userId, cancellationToken);
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
            request.Description,
            userId
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

    public async Task<Result> UpdateAsync(Guid id, UpdateAccountTransferRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }

        // Validate new source account
        var newFromAccount = await _accountRepository.GetByIdAndUserAsync(request.FromFinancialAccountId, userId, cancellationToken);
        if (newFromAccount == null)
        {
            return Result.Failure(new Error("AccountTransfer.InvalidFromAccount", "Source account not found"));
        }

        if (!newFromAccount.IsActive)
        {
            return Result.Failure(new Error("AccountTransfer.InactiveFromAccount", "Source account is inactive"));
        }

        // Validate new destination account
        var newToAccount = await _accountRepository.GetByIdAndUserAsync(request.ToFinancialAccountId, userId, cancellationToken);
        if (newToAccount == null)
        {
            return Result.Failure(new Error("AccountTransfer.InvalidToAccount", "Destination account not found"));
        }

        if (!newToAccount.IsActive)
        {
            return Result.Failure(new Error("AccountTransfer.InactiveToAccount", "Destination account is inactive"));
        }

        // Reverse old transfer
        var oldFromAccount = await _accountRepository.GetByIdAndUserAsync(transfer.FromFinancialAccountId, userId, cancellationToken);
        if (oldFromAccount != null)
        {
            oldFromAccount.Credit(transfer.Amount); // Reverse debit
            await _accountRepository.UpdateAsync(oldFromAccount, cancellationToken);
        }

        var oldToAccount = await _accountRepository.GetByIdAndUserAsync(transfer.ToFinancialAccountId, userId, cancellationToken);
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

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (transfer == null)
        {
            return Result.Failure(new Error("AccountTransfer.NotFound", "Transfer not found"));
        }

        // Reverse transfer: credit source, debit destination
        var fromAccount = await _accountRepository.GetByIdAndUserAsync(transfer.FromFinancialAccountId, userId, cancellationToken);
        if (fromAccount != null)
        {
            fromAccount.Credit(transfer.Amount);
            await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
        }

        var toAccount = await _accountRepository.GetByIdAndUserAsync(transfer.ToFinancialAccountId, userId, cancellationToken);
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
