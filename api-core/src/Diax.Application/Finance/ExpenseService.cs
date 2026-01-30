using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class ExpenseService : IApplicationService
{
    private readonly IExpenseRepository _repository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(
        IExpenseRepository repository,
        IFinancialAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<ExpenseResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var expenses = await _repository.GetAllAsync(cancellationToken);
        var response = expenses.Select(MapToResponse);
        return Result<IEnumerable<ExpenseResponse>>.Success(response);
    }

    public async Task<Result<ExpenseResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
        {
            return Result.Failure<ExpenseResponse>(new Error("Expense.NotFound", "Expense not found"));
        }
        return Result<ExpenseResponse>.Success(MapToResponse(expense));
    }

    public async Task<Result<IEnumerable<ExpenseResponse>>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var expenses = await _repository.GetByMonthAsync(year, month, cancellationToken);
        var response = expenses.Select(MapToResponse);
        return Result<IEnumerable<ExpenseResponse>>.Success(response);
    }

    public async Task<Result<PagedResult<ExpenseResponse>>> GetPagedAsync(ExpensePagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Expense, bool>> predicate = e =>
            (!request.StartDate.HasValue || e.Date >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || e.Date <= request.EndDate.Value) &&
            (!request.CategoryId.HasValue || e.ExpenseCategoryId == request.CategoryId.Value) &&
            (!request.FinancialAccountId.HasValue || e.FinancialAccountId == request.FinancialAccountId.Value) &&
            (!request.MinAmount.HasValue || e.Amount >= request.MinAmount.Value) &&
            (!request.MaxAmount.HasValue || e.Amount <= request.MaxAmount.Value) &&
            (!request.Status.HasValue || e.Status == request.Status.Value) &&
            (string.IsNullOrWhiteSpace(request.Search) || e.Description.Contains(request.Search));

        Func<IQueryable<Expense>, IOrderedQueryable<Expense>> orderBy = request.SortBy?.ToLower() switch
        {
            "date" => q => request.SortDescending ? q.OrderByDescending(e => e.Date) : q.OrderBy(e => e.Date),
            "amount" => q => request.SortDescending ? q.OrderByDescending(e => e.Amount) : q.OrderBy(e => e.Amount),
            "description" => q => request.SortDescending ? q.OrderByDescending(e => e.Description) : q.OrderBy(e => e.Description),
            _ => q => q.OrderByDescending(e => e.Date)
        };

        var pagedExpenses = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            predicate,
            orderBy,
            cancellationToken);

        var responseItems = pagedExpenses.Items.Select(MapToResponse);
        var response = new PagedResult<ExpenseResponse>(
            responseItems,
            pagedExpenses.TotalCount,
            pagedExpenses.Page,
            pagedExpenses.PageSize);

        return Result<PagedResult<ExpenseResponse>>.Success(response);
    }

    public async Task<Result<Guid>> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        // For cash payments, validate account exists and is active
        if (request.PaymentMethod != PaymentMethod.CreditCard && request.FinancialAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(request.FinancialAccountId.Value, cancellationToken);
            if (account == null)
            {
                return Result.Failure<Guid>(new Error("Expense.InvalidAccount", "Financial account not found"));
            }

            if (!account.IsActive)
            {
                return Result.Failure<Guid>(new Error("Expense.InactiveAccount", "Financial account is inactive"));
            }
        }

        // Domain entity will validate payment method constraints
        var expense = new Expense(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.ExpenseCategoryId,
            request.IsRecurring,
            request.CreditCardId,
            request.CreditCardInvoiceId,
            request.FinancialAccountId,
            request.Status,
            request.PaidDate
        );

        // Debit account for cash expenses
        if (request.PaymentMethod != PaymentMethod.CreditCard && request.FinancialAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(request.FinancialAccountId.Value, cancellationToken);
            if (account != null)
            {
                account.Debit(request.Amount);
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
        }

        await _repository.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(expense.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
        {
            return Result.Failure(new Error("Expense.NotFound", "Expense not found"));
        }

        // For cash payments, validate new account exists and is active
        if (request.PaymentMethod != PaymentMethod.CreditCard && request.FinancialAccountId.HasValue)
        {
            var newAccount = await _accountRepository.GetByIdAsync(request.FinancialAccountId.Value, cancellationToken);
            if (newAccount == null)
            {
                return Result.Failure(new Error("Expense.InvalidAccount", "Financial account not found"));
            }

            if (!newAccount.IsActive)
            {
                return Result.Failure(new Error("Expense.InactiveAccount", "Financial account is inactive"));
            }
        }

        // Handle balance updates for cash expenses
        var wasCredit = expense.PaymentMethod == PaymentMethod.CreditCard;
        var isCreditNow = request.PaymentMethod == PaymentMethod.CreditCard;
        var accountChanged = expense.FinancialAccountId != request.FinancialAccountId;
        var amountChanged = expense.Amount != request.Amount;

        if (!wasCredit && expense.FinancialAccountId.HasValue && (accountChanged || amountChanged || isCreditNow))
        {
            // Reverse old expense from old account (credit back)
            var oldAccount = await _accountRepository.GetByIdAsync(expense.FinancialAccountId.Value, cancellationToken);
            if (oldAccount != null)
            {
                oldAccount.Credit(expense.Amount);
                await _accountRepository.UpdateAsync(oldAccount, cancellationToken);
            }
        }

        if (!isCreditNow && request.FinancialAccountId.HasValue)
        {
            // Apply new expense to new account (debit)
            var newAccount = await _accountRepository.GetByIdAsync(request.FinancialAccountId.Value, cancellationToken);
            if (newAccount != null)
            {
                newAccount.Debit(request.Amount);
                await _accountRepository.UpdateAsync(newAccount, cancellationToken);
            }
        }

        expense.Update(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.ExpenseCategoryId,
            request.IsRecurring,
            request.CreditCardId,
            request.CreditCardInvoiceId,
            request.FinancialAccountId,
            request.Status,
            request.PaidDate
        );

        await _repository.UpdateAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
        {
            return Result.Failure(new Error("Expense.NotFound", "Expense not found"));
        }

        // Reverse cash expense from account (credit back)
        if (expense.PaymentMethod != PaymentMethod.CreditCard && expense.FinancialAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(expense.FinancialAccountId.Value, cancellationToken);
            if (account != null)
            {
                account.Credit(expense.Amount);
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
        }

        await _repository.DeleteAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAsPaidAsync(Guid id, DateTime? paidDate = null, CancellationToken cancellationToken = default)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
        {
            return Result.Failure(new Error("Expense.NotFound", "Expense not found"));
        }

        expense.MarkAsPaid(paidDate);
        await _repository.UpdateAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAsPendingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken);
        if (expense == null)
        {
            return Result.Failure(new Error("Expense.NotFound", "Expense not found"));
        }

        expense.MarkAsPending();
        await _repository.UpdateAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<ExpenseResponse>>> GetByStatusAsync(ExpenseStatus status, CancellationToken cancellationToken = default)
    {
        var allExpenses = await _repository.GetAllAsync(cancellationToken);
        var filteredExpenses = allExpenses.Where(e => e.Status == status);
        var response = filteredExpenses.Select(MapToResponse);
        return Result<IEnumerable<ExpenseResponse>>.Success(response);
    }

    private static ExpenseResponse MapToResponse(Expense expense)
    {
        return new ExpenseResponse(
            expense.Id,
            expense.Description,
            expense.Amount,
            expense.Date,
            expense.PaymentMethod,
            expense.ExpenseCategoryId,
            expense.ExpenseCategory?.Name,
            expense.IsRecurring,
            expense.CreditCardId,
            expense.CreditCardInvoiceId,
            expense.FinancialAccountId,
            expense.Status,
            expense.PaidDate,
            expense.CreatedAt,
            expense.UpdatedAt
        );
    }
}
