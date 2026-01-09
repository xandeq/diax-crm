using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class ExpenseService : IApplicationService
{
    private readonly IExpenseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(IExpenseRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
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

    public async Task<Result<Guid>> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = new Expense(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.Category,
            request.IsRecurring,
            request.CreditCardId,
            request.CreditCardInvoiceId,
            request.FinancialAccountId,
            request.Status,
            request.PaidDate
        );

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

        expense.Update(
            request.Description,
            request.Amount,
            request.Date,
            request.PaymentMethod,
            request.Category,
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
            expense.Category,
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
