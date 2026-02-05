using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;
using System.Linq.Expressions;

namespace Diax.Application.Finance;

public class IncomeService : IApplicationService
{
    private readonly IIncomeRepository _repository;
    private readonly IIncomeCategoryRepository _categoryRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IImportedTransactionRepository _importedTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IncomeService(
        IIncomeRepository repository,
        IIncomeCategoryRepository categoryRepository,
        IFinancialAccountRepository accountRepository,
        IImportedTransactionRepository importedTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _importedTransactionRepository = importedTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<IncomeResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var incomes = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var response = incomes.Select(MapToResponse);
        return Result<IEnumerable<IncomeResponse>>.Success(response);
    }

    public async Task<Result<IncomeResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (income == null)
        {
            return Result.Failure<IncomeResponse>(new Error("Income.NotFound", "Income not found"));
        }
        return Result<IncomeResponse>.Success(MapToResponse(income));
    }

    public async Task<Result<IEnumerable<IncomeResponse>>> GetByMonthAsync(int year, int month, Guid userId, CancellationToken cancellationToken = default)
    {
        var incomes = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        var filteredIncomes = incomes.Where(i => i.Date.Year == year && i.Date.Month == month);
        var response = filteredIncomes.Select(MapToResponse);
        return Result<IEnumerable<IncomeResponse>>.Success(response);
    }

    public async Task<Result<PagedResult<IncomeResponse>>> GetPagedAsync(IncomePagedRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        Expression<Func<Income, bool>> predicate = i =>
            i.UserId == userId &&
            (!request.StartDate.HasValue || i.Date >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || i.Date <= request.EndDate.Value) &&
            (!request.CategoryId.HasValue || i.IncomeCategoryId == request.CategoryId.Value) &&
            (!request.FinancialAccountId.HasValue || i.FinancialAccountId == request.FinancialAccountId.Value) &&
            (!request.MinAmount.HasValue || i.Amount >= request.MinAmount.Value) &&
            (!request.MaxAmount.HasValue || i.Amount <= request.MaxAmount.Value) &&
            (string.IsNullOrWhiteSpace(request.Search) || i.Description.Contains(request.Search));

        Func<IQueryable<Income>, IOrderedQueryable<Income>> orderBy = request.SortBy?.ToLower() switch
        {
            "date" => q => request.SortDescending ? q.OrderByDescending(i => i.Date) : q.OrderBy(i => i.Date),
            "amount" => q => request.SortDescending ? q.OrderByDescending(i => i.Amount) : q.OrderBy(i => i.Amount),
            "description" => q => request.SortDescending ? q.OrderByDescending(i => i.Description) : q.OrderBy(i => i.Description),
            _ => q => q.OrderByDescending(i => i.Date)
        };

        var pagedIncomes = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            predicate,
            orderBy,
            cancellationToken);

        var responseItems = pagedIncomes.Items.Select(MapToResponse);
        var response = new PagedResult<IncomeResponse>(
            responseItems,
            pagedIncomes.TotalCount,
            pagedIncomes.Page,
            pagedIncomes.PageSize);

        return Result<PagedResult<IncomeResponse>>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateIncomeRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.IncomeCategoryId, cancellationToken);
        if (category == null || !category.IsActive)
        {
            return Result.Failure<Guid>(new Error("Income.InvalidCategory", "Invalid or inactive income category"));
        }

        // Validate financial account exists
        var account = await _accountRepository.GetByIdAndUserAsync(request.FinancialAccountId, userId, cancellationToken);
        if (account == null)
        {
            return Result.Failure<Guid>(new Error("Income.InvalidAccount", "Financial account not found"));
        }

        if (!account.IsActive)
        {
            return Result.Failure<Guid>(new Error("Income.InactiveAccount", "Financial account is inactive"));
        }

        var income = new Income(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.IncomeCategoryId,
            request.IsRecurring,
            request.FinancialAccountId,
            userId
        );

        // Credit account balance
        account.Credit(request.Amount);

        await _repository.AddAsync(income, cancellationToken);
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(income.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateIncomeRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (income == null)
        {
            return Result.Failure(new Error("Income.NotFound", "Income not found"));
        }

        var category = await _categoryRepository.GetByIdAsync(request.IncomeCategoryId, cancellationToken);
        if (category == null || !category.IsActive)
        {
            return Result.Failure(new Error("Income.InvalidCategory", "Invalid or inactive income category"));
        }

        // Validate new financial account exists
        var newAccount = await _accountRepository.GetByIdAndUserAsync(request.FinancialAccountId, userId, cancellationToken);
        if (newAccount == null)
        {
            return Result.Failure(new Error("Income.InvalidAccount", "Financial account not found"));
        }

        if (!newAccount.IsActive)
        {
            return Result.Failure(new Error("Income.InactiveAccount", "Financial account is inactive"));
        }

        // If account or amount changed, reverse old balance and apply new
        if (income.FinancialAccountId != request.FinancialAccountId || income.Amount != request.Amount)
        {
            var oldAccount = await _accountRepository.GetByIdAndUserAsync(income.FinancialAccountId, userId, cancellationToken);
            if (oldAccount != null)
            {
                // Reverse old income from old account
                oldAccount.Debit(income.Amount);
                await _accountRepository.UpdateAsync(oldAccount, cancellationToken);
            }

            // Apply new income to new account
            newAccount.Credit(request.Amount);
            await _accountRepository.UpdateAsync(newAccount, cancellationToken);
        }

        income.Update(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.IncomeCategoryId,
            request.IsRecurring,
            request.FinancialAccountId
        );

        await _repository.UpdateAsync(income, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (income == null)
        {
            return Result.Failure(new Error("Income.NotFound", "Income not found"));
        }

        // Search for linked imported transactions and reset them
        var linkedTransactions = await _importedTransactionRepository.GetByIncomeIdAsync(id, cancellationToken);
        foreach (var transaction in linkedTransactions)
        {
            transaction.Reset();
            await _importedTransactionRepository.UpdateAsync(transaction, cancellationToken);
        }

        // Reverse income from account (debit the amount)
        var account = await _accountRepository.GetByIdAndUserAsync(income.FinancialAccountId, userId, cancellationToken);
        if (account != null)
        {
            account.Debit(income.Amount);
            await _accountRepository.UpdateAsync(account, cancellationToken);
        }

        await _repository.DeleteAsync(income, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<BulkDeleteResponse>> DeleteRangeAsync(BulkDeleteRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return Result<BulkDeleteResponse>.Success(new BulkDeleteResponse(true, 0, 0, new List<string>()));
        }

        if (request.Ids.Count > 100)
        {
            return Result.Failure<BulkDeleteResponse>(new Error("General.BatchLimitExceeded", "O limite máximo é de 100 itens por exclusão."));
        }

        return await _unitOfWork.ExecuteStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                int deletedCount = 0;
                foreach (var id in request.Ids)
                {
                    var result = await DeleteAsync(id, userId, ct);
                    if (!result.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result.Failure<BulkDeleteResponse>(result.Error);
                    }
                    deletedCount++;
                }

                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<BulkDeleteResponse>.Success(new BulkDeleteResponse(true, deletedCount, 0, new List<string>()));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result.Failure<BulkDeleteResponse>(new Error("General.BulkDeleteError", "Ocorreu um erro ao excluir os itens em massa: " + ex.Message));
            }
        }, cancellationToken);
    }

    private static IncomeResponse MapToResponse(Income income)
    {
        return new IncomeResponse(
            income.Id,
            income.Description,
            income.Amount,
            income.Date,
            income.PaymentMethod,
            income.IncomeCategoryId,
            income.IncomeCategory?.Name,
            income.IsRecurring,
            income.FinancialAccountId,
            income.FinancialAccount?.Name,
            income.CreatedAt,
            income.UpdatedAt
        );
    }
}
