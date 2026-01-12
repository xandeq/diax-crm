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

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all financial accounts");
            var accounts = await _repository.GetAllAsync(cancellationToken);
            var response = accounts.Select(MapToResponse);
            _logger.LogInformation("Successfully retrieved {Count} financial accounts", accounts.Count);
            return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial accounts");
            return Result.Failure<IEnumerable<FinancialAccountResponse>>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve financial accounts. Please check server logs for details."));
        }
    }

    public async Task<Result<IEnumerable<FinancialAccountResponse>>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching active financial accounts");
            var accounts = await _repository.GetActiveAccountsAsync(cancellationToken);
            var response = accounts.Select(MapToResponse);
            _logger.LogInformation("Successfully retrieved {Count} active financial accounts", accounts.Count);
            return Result<IEnumerable<FinancialAccountResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active financial accounts");
            return Result.Failure<IEnumerable<FinancialAccountResponse>>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve active financial accounts. Please check server logs for details."));
        }
    }

    public async Task<Result<FinancialAccountResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching financial account with ID: {AccountId}", id);
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found", id);
                return Result.Failure<FinancialAccountResponse>(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }
            _logger.LogInformation("Successfully retrieved financial account {AccountId}", id);
            return Result<FinancialAccountResponse>.Success(MapToResponse(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial account with ID: {AccountId}", id);
            return Result.Failure<FinancialAccountResponse>(
                new Error("FinancialAccount.QueryFailed", "Failed to retrieve financial account. Please check server logs for details."));
        }
    }

    public async Task<Result<Guid>> CreateAsync(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating financial account: {AccountName}", request.Name);
            var account = new FinancialAccount(
                request.Name,
                request.AccountType,
                request.InitialBalance,
                request.IsActive
            );

            await _repository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created financial account {AccountId}", account.Id);
            return Result<Guid>.Success(account.Id);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid financial account data: {AccountName}", request.Name);
            return Result.Failure<Guid>(new Error("FinancialAccount.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create financial account: {AccountName}", request.Name);
            return Result.Failure<Guid>(
                new Error("FinancialAccount.CreateFailed", "Failed to create financial account. Please check server logs for details."));
        }
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating financial account {AccountId}", id);
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found for update", id);
                return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }

            account.Update(
                request.Name,
                request.AccountType,
                request.IsActive
            );

            await _repository.UpdateAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated financial account {AccountId}", id);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update data for financial account {AccountId}", id);
            return Result.Failure(new Error("FinancialAccount.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update financial account {AccountId}", id);
            return Result.Failure(
                new Error("FinancialAccount.UpdateFailed", "Failed to update financial account. Please check server logs for details."));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting financial account {AccountId}", id);
            var account = await _repository.GetByIdAsync(id, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Financial account with ID {AccountId} not found for deletion", id);
                return Result.Failure(new Error("FinancialAccount.NotFound", "Financial account not found"));
            }

            await _repository.DeleteAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted financial account {AccountId}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete financial account {AccountId}", id);
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
