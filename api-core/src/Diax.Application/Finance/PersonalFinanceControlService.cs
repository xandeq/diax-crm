using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Application.Finance;

public class PersonalFinanceControlService : IApplicationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardInvoiceRepository _creditCardInvoiceRepository;
    private readonly ILogger<PersonalFinanceControlService> _logger;

    public PersonalFinanceControlService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        ICreditCardRepository creditCardRepository,
        ICreditCardInvoiceRepository creditCardInvoiceRepository,
        ILogger<PersonalFinanceControlService> logger)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _creditCardRepository = creditCardRepository;
        _creditCardInvoiceRepository = creditCardInvoiceRepository;
        _logger = logger;
    }

    public async Task<Result<PersonalFinanceMonthResponse>> GetMonthAsync(
        int? year,
        int? month,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var targetYear = year ?? now.Year;
            var targetMonth = month ?? now.Month;

            if (targetMonth < 1 || targetMonth > 12)
                return Result.Failure<PersonalFinanceMonthResponse>(new Error("PersonalFinance.InvalidMonth", "Mês deve estar entre 1 e 12"));

            if (targetYear < 2000)
                return Result.Failure<PersonalFinanceMonthResponse>(new Error("PersonalFinance.InvalidYear", "Ano deve ser 2000 ou maior"));

            var periodStart = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth), 23, 59, 59, DateTimeKind.Utc);

            var transactions = (await _transactionRepository.GetByMonthAsync(targetYear, targetMonth, userId, cancellationToken))
                .Where(t => t.Type != Domain.Finance.TransactionType.Transfer && t.Type != Domain.Finance.TransactionType.Ignored)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Description)
                .ToList();

            var recurringTemplates = await _recurringRepository.GetRecurringForMonthAsync(userId, targetMonth, targetYear);
            var cards = (await _creditCardRepository.GetAllByUserIdAsync(userId, cancellationToken)).ToList();
            var invoices = (await _creditCardInvoiceRepository.GetAllByUserIdAsync(userId, cancellationToken))
                .Where(i => i.ReferenceYear == targetYear && i.ReferenceMonth == targetMonth)
                .ToList();

            var warnings = new List<string>();
            warnings.AddRange(DetectDuplicateTransactions(transactions));
            warnings.AddRange(DetectInvalidStates(transactions));
            warnings.AddRange(DetectDuplicateRecurringTemplates(recurringTemplates));

            var recurringItems = recurringTemplates
                .SelectMany(template => ExpandRecurringTemplate(template, periodStart, periodEnd))
                .OrderBy(item => item.Date)
                .ThenBy(item => item.Priority)
                .ToList();

            var subscriptions = recurringItems
                .Where(item => item.IsSubscription)
                .ToList();

            var incomes = transactions
                .Where(t => t.Type == Domain.Finance.TransactionType.Income)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Description)
                .Select(MapTransaction)
                .ToList();

            var expenses = transactions
                .Where(t => t.Type == Domain.Finance.TransactionType.Expense)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Description)
                .Select(MapTransaction)
                .ToList();

            var cardSummaries = BuildCardSummaries(cards, invoices, transactions);
            var invoiceSummaries = invoices
                .OrderBy(i => i.DueDate)
                .ThenBy(i => i.ReferenceYear)
                .ThenBy(i => i.ReferenceMonth)
                .Select(invoice => new CreditCardInvoiceMonthlySummaryResponse
                {
                    Id = invoice.Id,
                    CreditCardGroupId = invoice.CreditCardGroupId,
                    CreditCardGroupName = invoice.CreditCardGroup?.Name ?? string.Empty,
                    ReferenceMonth = invoice.ReferenceMonth,
                    ReferenceYear = invoice.ReferenceYear,
                    ClosingDate = invoice.ClosingDate,
                    DueDate = invoice.DueDate,
                    TotalAmount = transactions.Where(t => t.CreditCardInvoiceId == invoice.Id).Sum(t => t.Amount),
                    IsPaid = invoice.IsPaid,
                    PaymentDate = invoice.PaymentDate,
                    PaidFromAccountId = invoice.PaidFromAccountId,
                    PaidFromAccountName = invoice.PaidFromAccount?.Name
                })
                .ToList();

            var actualIncome = incomes.Sum(i => i.Amount);
            var actualExpenses = expenses.Sum(e => e.Amount);
            var actualCreditExpenses = expenses.Where(e => e.PaymentMethod == PaymentMethod.CreditCard).Sum(e => e.Amount);
            var paidExpenses = expenses.Where(e => e.Status == TransactionStatus.Paid).Sum(e => e.Amount);
            var unpaidExpenses = expenses.Where(e => e.Status == TransactionStatus.Pending).Sum(e => e.Amount);
            var expensesWithCard = expenses.Where(e => e.PaymentMethod == PaymentMethod.CreditCard).Sum(e => e.Amount);
            var expensesWithoutCard = expenses.Where(e => e.PaymentMethod != PaymentMethod.CreditCard).Sum(e => e.Amount);

            var projectedIncome = actualIncome + recurringItems.Where(i => i.Type == PlannerTransactionType.Income).Sum(i => i.Amount);
            var projectedExpenses = actualExpenses + recurringItems.Where(i => i.Type == PlannerTransactionType.Expense).Sum(i => i.Amount);
            var projectedSubscriptions = subscriptions.Sum(i => i.Amount);

            var summary = new PersonalFinanceMonthSummaryResponse
            {
                TotalIncome = actualIncome,
                TotalExpenses = actualExpenses,
                TotalCreditExpenses = actualCreditExpenses,
                RemainingBalance = actualIncome - actualExpenses,
                PaidExpenses = paidExpenses,
                UnpaidExpenses = unpaidExpenses,
                ExpensesWithCard = expensesWithCard,
                ExpensesWithoutCard = expensesWithoutCard,
                ProjectedIncome = projectedIncome,
                ProjectedExpenses = projectedExpenses,
                ProjectedRemainingBalance = projectedIncome - projectedExpenses,
                SubscriptionTotal = projectedSubscriptions,
                PaidCount = expenses.Count(e => e.Status == TransactionStatus.Paid),
                UnpaidCount = expenses.Count(e => e.Status == TransactionStatus.Pending)
            };

            return Result<PersonalFinanceMonthResponse>.Success(new PersonalFinanceMonthResponse
            {
                Year = targetYear,
                Month = targetMonth,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Items = transactions.Select(MapTransaction).ToList(),
                Incomes = incomes,
                Expenses = expenses,
                RecurringItems = recurringItems,
                Subscriptions = subscriptions,
                CreditCards = cardSummaries,
                CreditCardInvoices = invoiceSummaries,
                Summary = summary,
                Warnings = warnings.Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build monthly personal finance view for user {UserId}", userId);
            return Result.Failure<PersonalFinanceMonthResponse>(
                new Error("PersonalFinance.MonthViewFailed", "Falha ao montar a visão mensal"));
        }
    }

    private static TransactionResponse MapTransaction(Transaction transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Amount,
            transaction.Date,
            transaction.Type,
            transaction.RawBankType,
            transaction.RawDescription,
            transaction.Details,
            transaction.PaymentMethod,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.IsRecurring,
            transaction.IsSubscription,
            transaction.FinancialAccountId,
            transaction.FinancialAccount?.Name,
            transaction.CreditCardId,
            transaction.CreditCard?.Name,
            transaction.CreditCardInvoiceId,
            transaction.Status,
            transaction.PaidDate,
            transaction.TransferGroupId,
            transaction.AccountTransferId,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }

    private static List<RecurringOccurrenceResponse> ExpandRecurringTemplate(
        RecurringTransaction recurring,
        DateTime periodStart,
        DateTime periodEnd)
    {
        return recurring.GetNextOccurrences(periodStart, periodEnd)
            .Select(date => new RecurringOccurrenceResponse
            {
                SourceRecurringTransactionId = recurring.Id,
                Description = recurring.Description,
                Details = recurring.Details,
                Amount = recurring.Amount,
                Date = date,
                Type = recurring.Type,
                ItemKind = recurring.ItemKind,
                FrequencyType = recurring.FrequencyType,
                DayOfMonth = recurring.DayOfMonth,
                PaymentMethod = recurring.PaymentMethod,
                CategoryId = recurring.CategoryId,
                CreditCardId = recurring.CreditCardId,
                FinancialAccountId = recurring.FinancialAccountId,
                IsSubscription = recurring.ItemKind == RecurringItemKind.Subscription,
                Priority = recurring.Priority,
                IsActive = recurring.IsActive
            })
            .ToList();
    }

    private static List<CreditCardMonthlySummaryResponse> BuildCardSummaries(
        IReadOnlyCollection<CreditCard> cards,
        IReadOnlyCollection<CreditCardInvoice> invoices,
        IReadOnlyCollection<Transaction> transactions)
    {
        var invoiceLookup = invoices
            .GroupBy(i => i.CreditCardGroupId)
            .ToDictionary(g => g.Key, g => g.First());

        return cards
            .Select(card =>
            {
                var cardTransactions = transactions.Where(t => t.CreditCardId == card.Id).ToList();

                invoiceLookup.TryGetValue(card.CreditCardGroupId ?? Guid.Empty, out var invoice);

                return new CreditCardMonthlySummaryResponse
                {
                    CreditCardId = card.Id,
                    CreditCardName = card.Name,
                    LastFourDigits = card.LastFourDigits,
                    CreditCardGroupId = card.CreditCardGroupId,
                    CreditCardGroupName = invoice?.CreditCardGroup?.Name,
                    TotalAmount = cardTransactions.Sum(t => t.Amount),
                    PaidAmount = cardTransactions.Where(t => t.Status == TransactionStatus.Paid).Sum(t => t.Amount),
                    PendingAmount = cardTransactions.Where(t => t.Status == TransactionStatus.Pending).Sum(t => t.Amount),
                    ExpenseCount = cardTransactions.Count,
                    HasInvoice = invoice != null,
                    InvoiceAmount = cardTransactions.Where(t => t.CreditCardInvoiceId == invoice?.Id).Sum(t => t.Amount),
                    InvoicePaid = invoice?.IsPaid ?? false,
                    InvoicePaymentDate = invoice?.PaymentDate,
                    InvoiceDueDate = invoice?.DueDate
                };
            })
            .OrderByDescending(card => card.TotalAmount)
            .ThenBy(card => card.CreditCardName)
            .ToList();
    }

    private List<string> DetectDuplicateTransactions(IEnumerable<Transaction> transactions)
    {
        var duplicates = transactions
            .GroupBy(t => new
            {
                t.Date.Date,
                t.Description,
                t.Amount,
                t.Type,
                t.PaymentMethod,
                t.FinancialAccountId,
                t.CreditCardId,
                t.CreditCardInvoiceId
            })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
            return new List<string>();

        return duplicates.Select(g =>
        {
            var sample = g.First();
            _logger.LogWarning(
                "Duplicate monthly transaction detected for user item on {Date}: {Description} ({Amount})",
                sample.Date,
                sample.Description,
                sample.Amount);
            return $"Duplicated item detected: {sample.Description} on {sample.Date:yyyy-MM-dd}";
        }).ToList();
    }

    private List<string> DetectDuplicateRecurringTemplates(IEnumerable<RecurringTransaction> templates)
    {
        var duplicates = templates
            .GroupBy(t => new
            {
                t.Type,
                t.ItemKind,
                t.Description,
                t.Details,
                t.Amount,
                t.DayOfMonth,
                t.FrequencyType,
                t.PaymentMethod,
                t.CreditCardId,
                t.FinancialAccountId
            })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
            return new List<string>();

        return duplicates.Select(g =>
        {
            var sample = g.First();
            _logger.LogWarning(
                "Duplicate recurring template detected: {Description} ({Amount})",
                sample.Description,
                sample.Amount);
            return $"Duplicated recurring template detected: {sample.Description}";
        }).ToList();
    }

    private List<string> DetectInvalidStates(IEnumerable<Transaction> transactions)
    {
        var warnings = new List<string>();

        foreach (var transaction in transactions)
        {
            if (string.IsNullOrWhiteSpace(transaction.Description))
            {
                warnings.Add($"Invalid empty description for transaction {transaction.Id}");
                _logger.LogWarning("Transaction {TransactionId} has an empty description", transaction.Id);
            }

            if (transaction.Amount <= 0)
            {
                warnings.Add($"Invalid amount for transaction {transaction.Id}");
                _logger.LogWarning("Transaction {TransactionId} has a non-positive amount {Amount}", transaction.Id, transaction.Amount);
            }

            if (transaction.Type == Domain.Finance.TransactionType.Expense && transaction.PaymentMethod == PaymentMethod.CreditCard)
            {
                if (!transaction.CreditCardId.HasValue)
                {
                    warnings.Add($"Expense {transaction.Id} uses credit card payment without CreditCardId");
                    _logger.LogWarning("Expense transaction {TransactionId} uses credit card payment without a CreditCardId", transaction.Id);
                }
            }

            if (transaction.Type == Domain.Finance.TransactionType.Expense && transaction.PaymentMethod != PaymentMethod.CreditCard && !transaction.FinancialAccountId.HasValue)
            {
                warnings.Add($"Expense {transaction.Id} is missing FinancialAccountId");
                _logger.LogWarning("Expense transaction {TransactionId} is missing a FinancialAccountId", transaction.Id);
            }

            if (transaction.Type == Domain.Finance.TransactionType.Income && !transaction.FinancialAccountId.HasValue)
            {
                warnings.Add($"Income {transaction.Id} is missing FinancialAccountId");
                _logger.LogWarning("Income transaction {TransactionId} is missing a FinancialAccountId", transaction.Id);
            }
        }

        return warnings;
    }
}
