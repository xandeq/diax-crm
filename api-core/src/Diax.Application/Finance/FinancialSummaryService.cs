using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class FinancialSummaryService : IApplicationService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly ICreditCardInvoiceRepository _invoiceRepository;

    public FinancialSummaryService(
        IExpenseRepository expenseRepository,
        IIncomeRepository incomeRepository,
        ICreditCardInvoiceRepository invoiceRepository)
    {
        _expenseRepository = expenseRepository;
        _incomeRepository = incomeRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<FinancialSummaryResponse>> GetSummaryAsync(
        FinancialSummaryRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? DateTime.MinValue;
        var endDate = request.EndDate ?? DateTime.MaxValue;

        // Get all expenses for user
        var allExpenses = await _expenseRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var expensesInPeriod = allExpenses
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .ToList();

        // Get all incomes for user
        var allIncomes = await _incomeRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var incomesInPeriod = allIncomes
            .Where(i => i.Date >= startDate && i.Date <= endDate)
            .ToList();

        // Get all invoices for user to find unpaid ones
        var allInvoices = await _invoiceRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var unpaidInvoices = allInvoices.Where(inv => !inv.IsPaid).ToList();

        // Calculate totals
        var totalIncome = incomesInPeriod.Sum(i => i.Amount);
        var totalExpenses = expensesInPeriod.Sum(e => e.Amount);

        var paidExpenses = expensesInPeriod.Where(e => e.Status == ExpenseStatus.Paid).ToList();
        var pendingExpenses = expensesInPeriod.Where(e => e.Status == ExpenseStatus.Pending).ToList();

        var totalPaidExpenses = paidExpenses.Sum(e => e.Amount);
        var totalPendingExpenses = pendingExpenses.Sum(e => e.Amount);

        // Calculate pending cash (non-credit-card pending expenses)
        var pendingCash = pendingExpenses
            .Where(e => e.PaymentMethod != PaymentMethod.CreditCard)
            .Sum(e => e.Amount);

        // Calculate pending credit (unpaid invoices)
        var pendingCredit = unpaidInvoices.Sum(inv => inv.GetTotalAmount());

        // Calculate cash flows
        var netCashFlow = totalIncome - totalPaidExpenses;
        var projectedCashFlow = totalIncome - totalExpenses;

        var summary = new FinancialSummaryResponse
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            TotalPaidExpenses = totalPaidExpenses,
            TotalPendingExpenses = totalPendingExpenses,
            PendingCash = pendingCash,
            PendingCredit = pendingCredit,
            NetCashFlow = netCashFlow,
            ProjectedCashFlow = projectedCashFlow,
            TotalExpenseCount = expensesInPeriod.Count,
            PaidExpenseCount = paidExpenses.Count,
            PendingExpenseCount = pendingExpenses.Count,
            UnpaidInvoiceCount = unpaidInvoices.Count
        };

        return Result<FinancialSummaryResponse>.Success(summary);
    }
}
