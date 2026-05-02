using Diax.Application.Common;
using Diax.Application.Finance;
using Diax.Application.Finance.Planner;
using Diax.Application.Integrations.Dtos;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;
using DomainTransactionType = Diax.Domain.Finance.TransactionType;

namespace Diax.Application.Integrations;

public interface ICashFlowProjectionIntegrationService
{
    Task<Result<CashFlowProjectionResponse>> GetProjectionAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

public class CashFlowProjectionIntegrationService : ICashFlowProjectionIntegrationService, IApplicationService
{
    private const decimal BigOutflowThreshold = 1000m;

    private readonly IFinancialAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly PersonalFinanceControlService _personalFinanceService;
    private readonly CashFlowProjectionService _cashFlowService;
    private readonly ILogger<CashFlowProjectionIntegrationService> _logger;

    public CashFlowProjectionIntegrationService(
        IFinancialAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        PersonalFinanceControlService personalFinanceService,
        CashFlowProjectionService cashFlowService,
        ILogger<CashFlowProjectionIntegrationService> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _personalFinanceService = personalFinanceService;
        _cashFlowService = cashFlowService;
        _logger = logger;
    }

    public async Task<Result<CashFlowProjectionResponse>> GetProjectionAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fromDate > toDate)
                return Result.Failure<CashFlowProjectionResponse>(
                    new Error("Integrations.InvalidDateRange", "fromDate must be on or before toDate"));

            var fromUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            // 1. Current balance — sum of active financial accounts
            var accounts = await _accountRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var currentBalance = accounts.Where(a => a.IsActive).Sum(a => a.Balance);

            // 2. AvailableToInvest — derived from current month's PersonalControlSummary
            //    (TotalIncome − TotalExpenses − sum(card statements)), mirroring the
            //    PersonalFinanceController canonical formula.
            var now = DateTime.UtcNow;
            var monthResult = await _personalFinanceService.GetMonthAsync(now.Year, now.Month, userId, cancellationToken);
            decimal availableToInvest = 0m;
            if (monthResult.IsSuccess)
            {
                var month = monthResult.Value;
                var cardStatements = month.CreditCards.Sum(c => c.StatementAmount ?? 0m);
                availableToInvest = month.Summary.TotalIncome - month.Summary.TotalExpenses - cardStatements;
            }
            else
            {
                _logger.LogWarning("Could not load monthly summary for availableToInvest: {Error}", monthResult.Error.Message);
            }

            // 3. Real pending transactions in window (excluding Transfer/Ignored)
            var realTx = await _transactionRepository.GetByDateRangeAsync(fromUtc, toUtc, userId, cancellationToken);
            var pendingReal = realTx
                .Where(t => t.Status == TransactionStatus.Pending
                         && t.Type != DomainTransactionType.Transfer
                         && t.Type != DomainTransactionType.Ignored)
                .ToList();

            var projected = pendingReal.Select(t => new ProjectedTransaction
            {
                SimulationId = Guid.Empty,
                Type = t.Type == DomainTransactionType.Income
                    ? PlannerTransactionType.Income
                    : PlannerTransactionType.Expense,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                CategoryId = t.CategoryId ?? Guid.Empty,
                PaymentMethod = t.PaymentMethod,
                Priority = 50,
                Source = ProjectionSource.Recurring,
                SourceId = t.Id,
                CreditCardId = t.CreditCardId,
                FinancialAccountId = t.FinancialAccountId,
                Status = ProjectedStatus.Planned
            }).ToList();

            // 4. Recurring template occurrences in range (deduped against real txs)
            var recurrings = await _recurringRepository.GetAllByUserIdAsync(userId);
            var realKey = pendingReal
                .Select(t => (t.Date.Date, t.Description, t.Amount))
                .ToHashSet();

            foreach (var r in recurrings.Where(r => r.IsActive))
            {
                var occurrences = r.GetNextOccurrences(fromUtc, toUtc);
                foreach (var date in occurrences)
                {
                    if (realKey.Contains((date.Date, r.Description, r.Amount)))
                        continue;

                    projected.Add(new ProjectedTransaction
                    {
                        SimulationId = Guid.Empty,
                        Type = r.Type,
                        Description = r.Description,
                        Amount = r.Amount,
                        Date = date,
                        CategoryId = r.CategoryId,
                        PaymentMethod = r.PaymentMethod,
                        Priority = r.Priority,
                        Source = ProjectionSource.Recurring,
                        SourceId = r.Id,
                        CreditCardId = r.CreditCardId,
                        FinancialAccountId = r.FinancialAccountId,
                        Status = ProjectedStatus.Planned
                    });
                }
            }

            projected = projected.OrderBy(p => p.Date).ThenBy(p => p.Priority).ToList();

            // 5. Daily projection — reuse canonical service
            var dailyBalances = _cashFlowService.ProjectDailyBalances(currentBalance, projected, Guid.Empty);

            // 6. Next big outflow (≥ R$ 1.000) within window
            var bigOutflow = projected
                .Where(p => p.Type == PlannerTransactionType.Expense && p.Amount >= BigOutflowThreshold)
                .OrderBy(p => p.Date)
                .FirstOrDefault();

            var response = new CashFlowProjectionResponse(
                currentBalance,
                availableToInvest,
                fromUtc,
                toUtc,
                bigOutflow == null
                    ? null
                    : new NextBigOutflow(bigOutflow.Date, bigOutflow.Amount, bigOutflow.Description),
                dailyBalances.Select(d => new DailyBalanceItem(
                    d.Date,
                    d.OpeningBalance,
                    d.TotalIncome,
                    d.TotalExpenses,
                    d.ClosingBalance,
                    d.IsNegative,
                    d.HasHighPriorityExpense)).ToList()
            );

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute cash flow projection for user {UserId} {From}..{To}",
                userId, fromDate, toDate);
            return Result.Failure<CashFlowProjectionResponse>(
                new Error("Integrations.CashFlowFailed", "Failed to compute cash flow projection"));
        }
    }
}
