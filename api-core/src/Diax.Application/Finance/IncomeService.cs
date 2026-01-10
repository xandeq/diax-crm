using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class IncomeService : IApplicationService
{
    private readonly IIncomeRepository _repository;
    private readonly IIncomeCategoryRepository _categoryRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IncomeService(
        IIncomeRepository repository,
        IIncomeCategoryRepository categoryRepository,
        IFinancialAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<IncomeResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var incomes = await _repository.GetAllAsync(cancellationToken);
        var response = incomes.Select(MapToResponse);
        return Result<IEnumerable<IncomeResponse>>.Success(response);
    }

    public async Task<Result<IncomeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAsync(id, cancellationToken);
        if (income == null)
        {
            return Result.Failure<IncomeResponse>(new Error("Income.NotFound", "Income not found"));
        }
        return Result<IncomeResponse>.Success(MapToResponse(income));
    }

    public async Task<Result<IEnumerable<IncomeResponse>>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var incomes = await _repository.GetByMonthAsync(year, month, cancellationToken);
        var response = incomes.Select(MapToResponse);
        return Result<IEnumerable<IncomeResponse>>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateIncomeRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.IncomeCategoryId, cancellationToken);
        if (category == null || !category.IsActive)
        {
            return Result.Failure<Guid>(new Error("Income.InvalidCategory", "Invalid or inactive income category"));
        }

        // Validate financial account exists
        var account = await _accountRepository.GetByIdAsync(request.FinancialAccountId, cancellationToken);
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
            request.FinancialAccountId
        );

        // Credit account balance
        account.Credit(request.Amount);

        await _repository.AddAsync(income, cancellationToken);
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(income.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateIncomeRequest request, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAsync(id, cancellationToken);
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
        var newAccount = await _accountRepository.GetByIdAsync(request.FinancialAccountId, cancellationToken);
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
            var oldAccount = await _accountRepository.GetByIdAsync(income.FinancialAccountId, cancellationToken);
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

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var income = await _repository.GetByIdAsync(id, cancellationToken);
        if (income == null)
        {
            return Result.Failure(new Error("Income.NotFound", "Income not found"));
        }

        // Reverse income from account (debit the amount)
        var account = await _accountRepository.GetByIdAsync(income.FinancialAccountId, cancellationToken);
        if (account != null)
        {
            account.Debit(income.Amount);
            await _accountRepository.UpdateAsync(account, cancellationToken);
        }

        await _repository.DeleteAsync(income, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
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
