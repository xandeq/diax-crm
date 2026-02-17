using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

/// <summary>
/// Serviço de resumo financeiro.
/// Consulta as tabelas unificadas (Transactions) + legadas (Incomes/Expenses)
/// para montar o resumo consolidado. Transferências e Ignoradas são excluídas.
/// </summary>
public class FinancialSummaryService : IApplicationService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly ICreditCardInvoiceRepository _invoiceRepository;
    private readonly ITransactionRepository _transactionRepository;

    public FinancialSummaryService(
        IExpenseRepository expenseRepository,
        IIncomeRepository incomeRepository,
        ICreditCardInvoiceRepository invoiceRepository,
        ITransactionRepository transactionRepository)
    {
        _expenseRepository = expenseRepository;
        _incomeRepository = incomeRepository;
        _invoiceRepository = invoiceRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<FinancialSummaryResponse>> GetSummaryAsync(
        FinancialSummaryRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? DateTime.MinValue;
        var endDate = request.EndDate ?? DateTime.MaxValue;

        // ── Legacy sources (Income/Expense tables) ──────────────
        var allExpenses = await _expenseRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var legacyExpensesInPeriod = allExpenses
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .ToList();

        var allIncomes = await _incomeRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var legacyIncomesInPeriod = allIncomes
            .Where(i => i.Date >= startDate && i.Date <= endDate)
            .ToList();

        // ── Unified source (Transaction table) ──────────────────
        // Exclui Transfer e Ignored — eles NÃO afetam o resumo financeiro
        var allTransactions = await _transactionRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var transactionsInPeriod = allTransactions
            .Where(t => t.Date >= startDate && t.Date <= endDate
                        && t.Type != TransactionType.Transfer
                        && t.Type != TransactionType.Ignored)
            .ToList();

        var unifiedIncomes = transactionsInPeriod.Where(t => t.Type == TransactionType.Income).ToList();
        var unifiedExpenses = transactionsInPeriod.Where(t => t.Type == TransactionType.Expense).ToList();

        // ── Invoices ────────────────────────────────────────────
        var allInvoices = await _invoiceRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var unpaidInvoices = allInvoices.Where(inv => !inv.IsPaid).ToList();

        // ── Calculate totals (legacy + unified) ─────────────────
        var totalIncome = legacyIncomesInPeriod.Sum(i => i.Amount)
                        + unifiedIncomes.Sum(t => t.Amount);

        var totalExpenses = legacyExpensesInPeriod.Sum(e => e.Amount)
                          + unifiedExpenses.Sum(t => t.Amount);

        // Paid/Pending - legacy
        var legacyPaid = legacyExpensesInPeriod.Where(e => e.Status == ExpenseStatus.Paid).ToList();
        var legacyPending = legacyExpensesInPeriod.Where(e => e.Status == ExpenseStatus.Pending).ToList();

        // Paid/Pending - unified
        var unifiedPaid = unifiedExpenses.Where(t => t.Status == TransactionStatus.Paid).ToList();
        var unifiedPending = unifiedExpenses.Where(t => t.Status == TransactionStatus.Pending).ToList();

        var totalPaidExpenses = legacyPaid.Sum(e => e.Amount) + unifiedPaid.Sum(t => t.Amount);
        var totalPendingExpenses = legacyPending.Sum(e => e.Amount) + unifiedPending.Sum(t => t.Amount);

        // Pending cash (non-credit-card pending expenses)
        var pendingCash = legacyPending.Where(e => e.PaymentMethod != PaymentMethod.CreditCard).Sum(e => e.Amount)
                        + unifiedPending.Where(t => t.PaymentMethod != PaymentMethod.CreditCard).Sum(t => t.Amount);

        // Pending credit (unpaid invoices)
        var pendingCredit = unpaidInvoices.Sum(inv => inv.GetTotalAmount());

        // Cash flows
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
            TotalExpenseCount = legacyExpensesInPeriod.Count + unifiedExpenses.Count,
            PaidExpenseCount = legacyPaid.Count + unifiedPaid.Count,
            PendingExpenseCount = legacyPending.Count + unifiedPending.Count,
            UnpaidInvoiceCount = unpaidInvoices.Count
        };

        return Result<FinancialSummaryResponse>.Success(summary);
    }
}
