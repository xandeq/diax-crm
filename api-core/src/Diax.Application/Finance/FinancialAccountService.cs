using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance;

public class FinancialAccountService : IApplicationService
{
    private readonly IFinancialAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FinancialAccountService> _logger;

    public FinancialAccountService(
        IFinancialAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<FinancialAccountService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all financial accounts for user {UserId}", userId);
            var accounts = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
            var response = accounts.Select(MapToResponse);
            _logger.LogInformation("Successfully retrieved {Count} financial accounts for user {UserId}", accounts.Count(), userId);
            return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial accounts for user {UserId}", userId);
            return Result.Failure<IEnumerable<FinancialAccountResponse>>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve financial accounts. Please check server logs for details."));
        }
    }

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetActiveAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching active financial accounts for user {UserId}", userId);
            var accounts = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
            var activeAccounts = accounts.Where(a => a.IsActive);
            var response = activeAccounts.Select(MapToResponse);
            _logger.LogInformation("Successfully retrieved {Count} active financial accounts for user {UserId}", activeAccounts.Count(), userId);
            return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active financial accounts for user {UserId}", userId);
            return Result.Failure<IEnumerable<FinancialAccountResponse>>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve active financial accounts. Please check server logs for details."));
        }
    }

    public async Task<Result<FinancialAccountResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching financial account with ID: {AccountId} for user {UserId}", id, userId);
            var account = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found for user {UserId}", id, userId);
                return Result.Failure<FinancialAccountResponse>(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }
            _logger.LogInformation("Successfully retrieved financial account {AccountId} for user {UserId}", id, userId);
            return Result<FinancialAccountResponse>.Success(MapToResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial account with ID: {AccountId} for user {UserId}", id, userId);
            return Result.Failure<FinancialAccountResponse>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve financial account. Please check server logs for details."));
        }
    }

    public async Task<Result<Guid>> CreateAsync(CreateFinancialAccountRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating financial account: {AccountName} for user {UserId}", request.Name, userId);
            var account = new FinancialAccount(
                request.Name,
                request.AccountType,
                request.InitialBalance,
                userId,
                request.IsActive
            );

            await _repository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created financial account {AccountId} for user {UserId}", account.Id, userId);
            return Result<Guid>.Success(account.Id);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid financial account data: {AccountName} for user {UserId}", request.Name, userId);
            return Result.Failure<Guid>(new Error("FinancialAccount.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create financial account: {AccountName} for user {UserId}", request.Name, userId);
            return Result.Failure<Guid>(
                new Error("FinancialAccount.CreateFailed", "Failed to create financial account. Please check server logs for details."));
        }
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateFinancialAccountRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating financial account {AccountId} for user {UserId}", id, userId);
            var account = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found for update for user {UserId}", id, userId);
                return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }

            account.Update(
                request.Name,
                request.AccountType,
                request.IsActive
            );

            await _repository.UpdateAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated financial account {AccountId} for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update data for financial account {AccountId} for user {UserId}", id, userId);
            return Result.Failure(new Error("FinancialAccount.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update financial account {AccountId} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("FinancialAccount.UpdateFailed", "Failed to update financial account. Please check server logs for details."));
        }
    }

    public async Task<Result> UpdateBalanceAsync(Guid id, decimal newBalance, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating balance for financial account {AccountId} for user {UserId}", id, userId);
            var account = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (account == null)
                return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));

            account.UpdateBalance(newBalance);
            await _repository.UpdateAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated balance for financial account {AccountId} to {NewBalance}", id, newBalance);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update balance for financial account {AccountId}", id);
            return Result.Failure(new Error("FinancialAccount.UpdateFailed", "Failed to update balance."));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting financial account {AccountId} for user {UserId}", id, userId);
            var account = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found for deletion for user {UserId}", id, userId);
                return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }

            await _repository.DeleteAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted financial account {AccountId} for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete financial account {AccountId} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("FinancialAccount.DeleteFailed", "Failed to delete financial account. Please check server logs for details."));
        }
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
