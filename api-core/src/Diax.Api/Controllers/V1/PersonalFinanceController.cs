using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Diax.Infrastructure.Finance.Parsers;
using Microsoft.Extensions.Configuration;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/personal-control")]
[Produces("application/json")]
public class PersonalFinanceController : BaseApiController
{
    private readonly PersonalFinanceControlService _monthService;
    private readonly TransactionService _transactionService;
    private readonly RecurringTransactionService _recurringService;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly PdfFileParser _pdfParser;
    private readonly IGoogleSheetsService _sheetsService;
    private readonly string _sheetsSpreadsheetId;

    private static readonly string[] PortugueseMonths =
    [
        "JANEIRO", "FEVEREIRO", "MARÇO", "ABRIL", "MAIO", "JUNHO",
        "JULHO", "AGOSTO", "SETEMBRO", "OUTUBRO", "NOVEMBRO", "DEZEMBRO"
    ];

    private static string GetSheetTabName(int year, int month) =>
        $"{PortugueseMonths[month - 1]} {year}";

    public PersonalFinanceController(
        PersonalFinanceControlService monthService,
        TransactionService transactionService,
        RecurringTransactionService recurringService,
        IFinancialAccountRepository financialAccountRepository,
        ICreditCardRepository creditCardRepository,
        PdfFileParser pdfParser,
        IGoogleSheetsService sheetsService,
        IConfiguration configuration)
    {
        _monthService = monthService;
        _transactionService = transactionService;
        _recurringService = recurringService;
        _financialAccountRepository = financialAccountRepository;
        _creditCardRepository = creditCardRepository;
        _pdfParser = pdfParser;
        _sheetsService = sheetsService;
        _sheetsSpreadsheetId = configuration["GoogleSheets:SpreadsheetId"] ?? string.Empty;
    }

    private void SyncSheetsAsync(Guid id, Guid userId, int year, int month, bool isPaid, DateOnly? paymentDate)
    {
        if (string.IsNullOrEmpty(_sheetsSpreadsheetId)) return;
        var tabName = GetSheetTabName(year, month);
        _ = Task.Run(async () =>
        {
            var txResult = await _transactionService.GetByIdAsync(id, userId);
            if (!txResult.IsSuccess) return;
            await _sheetsService.UpdatePaymentStatusAsync(
                _sheetsSpreadsheetId, tabName, txResult.Value!.Description,
                isPaid, paymentDate);
        });
    }

    [HttpGet("months/{year:int}/{month:int}")]
    public async Task<IActionResult> GetMonth(int year, int month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.GetMonthAsync(year, month, userId.Value, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var response = MapMonthView(result.Value);
        return Ok(response);
    }

    [HttpGet("morning-briefing")]
    public async Task<IActionResult> GetMorningBriefing(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var now = DateTime.UtcNow;
        var monthResult = await _monthService.GetMonthAsync(now.Year, now.Month, userId.Value, cancellationToken);
        if (!monthResult.IsSuccess)
            return BadRequest(monthResult.Error);

        var month = monthResult.Value;
        var today = now.Date;
        var weekEnd = today.AddDays(7);

        var pendingDebitExpenses = month.Expenses
            .Where(e => e.Status == TransactionStatus.Pending && !e.IsSubscription)
            .ToList();

        var overdueList = pendingDebitExpenses
            .Where(e => e.Date.Date < today)
            .OrderBy(e => e.Date)
            .Select(e => new { id = e.Id, description = e.Description, amount = e.Amount, date = e.Date, daysOverdue = (int)(today - e.Date.Date).TotalDays })
            .ToList();

        var dueTodayList = pendingDebitExpenses
            .Where(e => e.Date.Date == today)
            .OrderBy(e => e.Description)
            .Select(e => new { id = e.Id, description = e.Description, amount = e.Amount, paymentMethod = e.PaymentMethod.ToString() })
            .ToList();

        var dueThisWeekList = pendingDebitExpenses
            .Where(e => e.Date.Date > today && e.Date.Date <= weekEnd)
            .OrderBy(e => e.Date)
            .Select(e => new { id = e.Id, description = e.Description, amount = e.Amount, date = e.Date, paymentMethod = e.PaymentMethod.ToString() })
            .ToList();

        var pendingSubscriptionsList = month.Items
            .Where(t => t.IsSubscription && t.Status == TransactionStatus.Pending)
            .OrderBy(t => t.Description)
            .Select(t => new { id = t.RecurringTransactionId, transactionId = t.Id, description = t.Description, amount = t.Amount, paymentType = t.PaymentMethod == PaymentMethod.CreditCard ? "credit" : "debit" })
            .ToList();

        var availableToInvest = month.Summary.TotalIncome
            - month.Summary.TotalExpenses
            - month.CreditCards.Sum(c => c.StatementAmount ?? 0m);

        return Ok(new
        {
            generatedAt = now,
            period = new { year = now.Year, month = now.Month, label = $"{now.Month:D2}/{now.Year}" },
            summary = new
            {
                totalIncome = month.Summary.TotalIncome,
                totalPaid = month.Summary.PaidExpenses,
                totalPending = month.Summary.UnpaidExpenses,
                remainingBalance = month.Summary.RemainingBalance,
                availableToInvest,
                paidCount = month.Summary.PaidCount,
                unpaidCount = month.Summary.UnpaidCount
            },
            alerts = new
            {
                hasUrgentItems = overdueList.Count > 0 || dueTodayList.Count > 0,
                overdueCount = overdueList.Count,
                overdueAmount = overdueList.Sum(e => e.amount),
                dueTodayCount = dueTodayList.Count,
                dueTodayAmount = dueTodayList.Sum(e => e.amount),
                dueThisWeekCount = dueThisWeekList.Count,
                dueThisWeekAmount = dueThisWeekList.Sum(e => e.amount),
                pendingSubscriptionsCount = pendingSubscriptionsList.Count,
                overdue = overdueList,
                dueToday = dueTodayList,
                dueThisWeek = dueThisWeekList,
                pendingSubscriptions = pendingSubscriptionsList
            }
        });
    }

    [HttpPost("incomes")]
    public async Task<IActionResult> CreateIncome([FromBody] PersonalControlIncomeRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var date = CreateDate(request.Year, request.Month, request.DayOfMonth);
        var defaultAccount = await GetDefaultAccountAsync(userId.Value, cancellationToken);
        if (defaultAccount == null)
            return BadRequest(new { message = "Nenhuma conta financeira ativa encontrada para lançar a receita." });

        var result = await _transactionService.CreateAsync(
            new CreateTransactionRequest(
                request.Name,
                request.Amount,
                date,
                Domain.Finance.TransactionType.Income,
                PaymentMethod.Pix,
                null,
                request.IsRecurring,
                defaultAccount.Id,
                null,
                null,
                TransactionStatus.Paid,
                request.PaymentDate,
                request.Details,
                false),
            userId.Value,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("incomes/{id:guid}")]
    public async Task<IActionResult> UpdateIncome(Guid id, [FromBody] PersonalControlIncomeRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var existing = await _transactionService.GetByIdAsync(id, userId.Value, cancellationToken);
        if (!existing.IsSuccess)
            return NotFound(existing.Error);

        var date = CreateDate(request.Year, request.Month, request.DayOfMonth);
        var accountId = existing.Value.FinancialAccountId ?? (await GetDefaultAccountAsync(userId.Value, cancellationToken))?.Id;
        if (!accountId.HasValue)
            return BadRequest(new { message = "Nenhuma conta financeira ativa encontrada para atualizar a receita." });

        var result = await _transactionService.UpdateAsync(
            id,
            new UpdateTransactionRequest(
                request.Name,
                request.Amount,
                date,
                PaymentMethod.Pix,
                existing.Value.CategoryId,
                request.IsRecurring,
                accountId,
                null,
                null,
                request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                request.PaymentDate,
                request.Details,
                false),
            userId.Value,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("incomes/{id:guid}")]
    public async Task<IActionResult> DeleteIncome(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("incomes/{id:guid}/status")]
    [HttpPost("incomes/{id:guid}/status")]
    public async Task<IActionResult> ToggleIncomeStatus(Guid id, [FromBody] TogglePersonalControlStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(id, userId.Value, cancellationToken);

        if (result.IsSuccess && request.Year.HasValue && request.Month.HasValue)
        {
            var pd = request.PaymentDate.HasValue ? DateOnly.FromDateTime(request.PaymentDate.Value) : (DateOnly?)null;
            SyncSheetsAsync(id, userId.Value, request.Year.Value, request.Month.Value, request.IsPaid, pd);
        }

        return HandleResult(result);
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] PersonalControlExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var date = CreateDate(request.Year, request.Month, request.DueDay);
        var paymentMethod = request.PaymentType == "credit" ? PaymentMethod.CreditCard : PaymentMethod.DebitCard;
        Guid? accountId = null;
        Guid? creditCardId = null;

        if (paymentMethod == PaymentMethod.CreditCard)
        {
            if (!Guid.TryParse(request.CreditCardId, out var parsedCardId))
                return BadRequest(new { message = "Cartão de crédito inválido." });

            var card = await _creditCardRepository.GetByIdAndUserAsync(parsedCardId, userId.Value, cancellationToken);
            if (card == null)
                return BadRequest(new { message = "Cartão de crédito não encontrado." });

            creditCardId = parsedCardId;
        }
        else
        {
            accountId = (await GetDefaultAccountAsync(userId.Value, cancellationToken))?.Id;
            if (!accountId.HasValue)
                return BadRequest(new { message = "Nenhuma conta financeira ativa encontrada para lançar a despesa." });
        }

        var result = await _transactionService.CreateAsync(
            new CreateTransactionRequest(
                request.Name,
                request.Amount,
                date,
                Domain.Finance.TransactionType.Expense,
                paymentMethod,
                null,
                false,
                accountId,
                creditCardId,
                null,
                request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                request.PaymentDate,
                request.Details,
                false,
                request.HasVariableAmount),
            userId.Value,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("expenses/{id:guid}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] PersonalControlExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var date = CreateDate(request.Year, request.Month, request.DueDay);
        var paymentMethod = request.PaymentType == "credit" ? PaymentMethod.CreditCard : PaymentMethod.DebitCard;
        Guid? accountId = null;
        Guid? creditCardId = null;

        if (paymentMethod == PaymentMethod.CreditCard)
        {
            if (!Guid.TryParse(request.CreditCardId, out var parsedCardId))
                return BadRequest(new { message = "Cartão de crédito inválido." });

            creditCardId = parsedCardId;
        }
        else
        {
            accountId = (await GetDefaultAccountAsync(userId.Value, cancellationToken))?.Id;
            if (!accountId.HasValue)
                return BadRequest(new { message = "Nenhuma conta financeira ativa encontrada para atualizar a despesa." });
        }

        var existing = await _transactionService.GetByIdAsync(id, userId.Value, cancellationToken);
        if (!existing.IsSuccess)
            return NotFound(existing.Error);

        var result = await _transactionService.UpdateAsync(
            id,
            new UpdateTransactionRequest(
                request.Name,
                request.Amount,
                date,
                paymentMethod,
                existing.Value.CategoryId,
                existing.Value.IsRecurring,
                accountId,
                creditCardId,
                existing.Value.CreditCardInvoiceId,
                request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                request.PaymentDate,
                request.Details,
                false,
                request.HasVariableAmount),
            userId.Value,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("expenses/{id:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("expenses/{id:guid}/status")]
    [HttpPost("expenses/{id:guid}/status")]
    public async Task<IActionResult> ToggleExpenseStatus(Guid id, [FromBody] TogglePersonalControlStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(id, userId.Value, cancellationToken);

        if (result.IsSuccess && request.Year.HasValue && request.Month.HasValue)
        {
            var pd = request.PaymentDate.HasValue ? DateOnly.FromDateTime(request.PaymentDate.Value) : (DateOnly?)null;
            SyncSheetsAsync(id, userId.Value, request.Year.Value, request.Month.Value, request.IsPaid, pd);
        }

        return HandleResult(result);
    }

    [HttpPost("import-sheet/{year:int}/{month:int}")]
    public async Task<IActionResult> ImportFromSheet(int year, int month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.ImportFromSheetAsync(year, month, userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] PersonalControlSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var recurringRequest = await BuildSubscriptionTemplateRequestAsync(request, userId.Value, cancellationToken);
        if (recurringRequest == null)
            return BadRequest(new { message = "Não foi possível criar a assinatura sem uma conta financeira ativa ou cartão válido." });

        var recurringResult = await _recurringService.CreateAsync(recurringRequest, userId.Value);
        if (!recurringResult.IsSuccess)
            return BadRequest(recurringResult.Error);

        var transactionResult = await _transactionService.CreateAsync(
            new CreateTransactionRequest(
                request.Name,
                request.Amount,
                CreateDate(request.Year, request.Month, 1),
                Domain.Finance.TransactionType.Expense,
                recurringRequest.PaymentMethod,
                null,
                true,
                recurringRequest.FinancialAccountId,
                recurringRequest.CreditCardId,
                null,
                request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                request.PaymentDate,
                request.Details,
                true,
                request.HasVariableAmount,
                recurringResult.Value.Id),
            userId.Value,
            cancellationToken);

        return transactionResult.IsSuccess ? Ok(recurringResult.Value.Id) : BadRequest(transactionResult.Error);
    }

    [HttpPut("subscriptions/{id:guid}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] PersonalControlSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var recurringRequest = await BuildSubscriptionTemplateUpdateRequestAsync(request, userId.Value, cancellationToken);
        if (recurringRequest == null)
            return BadRequest(new { message = "Não foi possível atualizar a assinatura sem uma conta financeira ativa ou cartão válido." });

        var result = await _recurringService.UpdateAsync(id, recurringRequest, userId.Value);
        if (!result.IsSuccess)
            return HandleResult(result);

        // Option A — propagate amount change to the existing Transaction for this month.
        // After the template update, occurrence.Amount = new value. The prefer-exact-then-fallback
        // below fails the exact match (Transaction still has old amount) and hits the fallback
        // (Description+Date), finding the Transaction. TransactionService.UpdateAsync then
        // reverses the old balance impact and applies the new one atomically.
        var monthView = await _monthService.GetMonthAsync(request.Year, request.Month, userId.Value, cancellationToken);
        if (monthView.IsSuccess)
        {
            var occurrence = monthView.Value.Subscriptions.FirstOrDefault(x => x.SourceRecurringTransactionId == id);
            if (occurrence != null)
            {
                var tx = monthView.Value.Items.FirstOrDefault(x => x.IsSubscription && x.RecurringTransactionId == id)
                    ?? monthView.Value.Items.FirstOrDefault(x =>
                        x.IsSubscription && x.Description == occurrence.Description
                        && x.Amount == occurrence.Amount && x.Date.Date == occurrence.Date.Date)
                    ?? monthView.Value.Items.FirstOrDefault(x =>
                        x.IsSubscription && x.Description == occurrence.Description
                        && x.Date.Date == occurrence.Date.Date);

                if (tx != null && tx.Amount != occurrence.Amount)
                {
                    await _transactionService.UpdateAsync(tx.Id,
                        new UpdateTransactionRequest(
                            tx.Description,
                            occurrence.Amount,
                            tx.Date,
                            tx.PaymentMethod,
                            tx.CategoryId,
                            tx.IsRecurring,
                            tx.FinancialAccountId,
                            tx.CreditCardId,
                            tx.CreditCardInvoiceId,
                            tx.Status,
                            tx.PaidDate,
                            tx.Details,
                            true,
                            occurrence.HasVariableAmount),
                        userId.Value,
                        cancellationToken);
                }
            }
        }

        return Ok();
    }

    [HttpDelete("subscriptions/{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.DeleteAsync(id, userId.Value);
        return HandleResult(result);
    }

    [HttpPatch("subscriptions/{id:guid}/status")]
    [HttpPost("subscriptions/{id:guid}/status")]
    public async Task<IActionResult> ToggleSubscriptionStatus(Guid id, [FromBody] TogglePersonalControlSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var monthView = await _monthService.GetMonthAsync(request.Year, request.Month, userId.Value, cancellationToken);
        if (!monthView.IsSuccess)
            return BadRequest(monthView.Error);

        var occurrence = monthView.Value.Subscriptions.FirstOrDefault(x => x.SourceRecurringTransactionId == id);
        if (occurrence == null)
            return NotFound(new { message = "Assinatura não encontrada para o período informado." });

        // Tier 1: FK-based match (new transactions from CreateSubscription / ToggleSubscriptionStatus
        // create path have RecurringTransactionId set). Tier 2: exact (Description, Amount, Date)
        // for legacy transactions without the FK. Tier 3: fuzzy (Description, Date) for
        // HasVariableAmount subscriptions where the template amount was edited after materialisation.
        var transaction = monthView.Value.Items.FirstOrDefault(x =>
                x.IsSubscription && x.RecurringTransactionId == id)
            ?? monthView.Value.Items.FirstOrDefault(x =>
                x.IsSubscription
                && x.Description == occurrence.Description
                && x.Amount == occurrence.Amount
                && x.Date.Date == occurrence.Date.Date)
            ?? monthView.Value.Items.FirstOrDefault(x =>
                x.IsSubscription
                && x.Description == occurrence.Description
                && x.Date.Date == occurrence.Date.Date);

        if (transaction == null)
        {
            var createResult = await _transactionService.CreateAsync(
                new CreateTransactionRequest(
                    occurrence.Description,
                    occurrence.Amount,
                    occurrence.Date,
                    Domain.Finance.TransactionType.Expense,
                    occurrence.PaymentMethod,
                    null,
                    true,
                    occurrence.PaymentMethod == PaymentMethod.CreditCard ? null : occurrence.FinancialAccountId,
                    occurrence.PaymentMethod == PaymentMethod.CreditCard ? occurrence.CreditCardId : null,
                    null,
                    request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                    request.PaymentDate,
                    occurrence.Details,
                    true,
                    occurrence.HasVariableAmount,
                    id),
                userId.Value,
                cancellationToken);

            if (!createResult.IsSuccess)
                return BadRequest(createResult.Error);

            if (string.IsNullOrEmpty(_sheetsSpreadsheetId) is false)
            {
                var pd = request.PaymentDate.HasValue ? DateOnly.FromDateTime(request.PaymentDate.Value) : (DateOnly?)null;
                var tabName = GetSheetTabName(request.Year, request.Month);
                _ = _sheetsService.UpdatePaymentStatusAsync(_sheetsSpreadsheetId, tabName, occurrence.Description, request.IsPaid, pd);
            }

            return Ok();
        }

        var toggleResult = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(transaction.Id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(transaction.Id, userId.Value, cancellationToken);

        if (toggleResult.IsSuccess)
        {
            var pd = request.PaymentDate.HasValue ? DateOnly.FromDateTime(request.PaymentDate.Value) : (DateOnly?)null;
            var tabName = GetSheetTabName(request.Year, request.Month);
            _ = _sheetsService.UpdatePaymentStatusAsync(_sheetsSpreadsheetId, tabName, occurrence.Description, request.IsPaid, pd);
        }

        return HandleResult(toggleResult);
    }

    private static object MapMonthView(PersonalFinanceMonthResponse source)
    {
        var subscriptionItems = source.Subscriptions.Select(subscription =>
        {
            // Tier 1: FK-based (transactions created after Sprint 4 carry RecurringTransactionId).
            // Tier 2: exact (Description, Amount, Date) — legacy transactions without the FK.
            // Tier 3: fuzzy (Description, Date) — HasVariableAmount where template amount was
            // edited after materialisation; without this the row shows "Pendente" forever and
            // the toggle endpoint silently creates duplicate Transactions on every click.
            var matchedTransaction = source.Items.FirstOrDefault(item =>
                    item.IsSubscription && item.RecurringTransactionId == subscription.SourceRecurringTransactionId)
                ?? source.Items.FirstOrDefault(item =>
                    item.IsSubscription
                    && item.Description == subscription.Description
                    && item.Amount == subscription.Amount
                    && item.Date.Date == subscription.Date.Date)
                ?? source.Items.FirstOrDefault(item =>
                    item.IsSubscription
                    && item.Description == subscription.Description
                    && item.Date.Date == subscription.Date.Date);

            return new
            {
                id = subscription.SourceRecurringTransactionId,
                name = subscription.Description,
                // Always display the template's Amount (subscription.Amount) — that's the
                // value the user just entered when editing the subscription. The Transaction's
                // own Amount may be stale because UpdateSubscription only writes the template;
                // it doesn't propagate to existing month Transactions. Showing matchedTransaction
                // .Amount here would surface the stale value and surprise the user.
                amount = subscription.Amount,
                billingFrequency = ToBillingFrequency(subscription.FrequencyType),
                paymentType = subscription.PaymentMethod == PaymentMethod.CreditCard ? "credit" : "debit",
                isPaid = matchedTransaction?.Status == TransactionStatus.Paid,
                paymentDate = matchedTransaction?.PaidDate,
                details = subscription.Details,
                creditCardId = subscription.CreditCardId,
                creditCardName = source.CreditCards.FirstOrDefault(card => card.CreditCardId == subscription.CreditCardId)?.CreditCardName,
                hasVariableAmount = subscription.HasVariableAmount,
                transactionId = matchedTransaction?.Id,
                createdAt = matchedTransaction?.CreatedAt,
                updatedAt = matchedTransaction?.UpdatedAt
            };
        }).ToList();

        return new
        {
            period = new
            {
                year = source.Year,
                month = source.Month,
                label = $"{source.Month:D2}/{source.Year}"
            },
            summary = new
            {
                totalIncome = source.Summary.TotalIncome,
                totalExpenses = source.Summary.TotalExpenses,
                totalCreditExpenses = source.Summary.TotalCreditExpenses,
                remainingBalance = source.Summary.RemainingBalance,
                paidAmount = source.Summary.PaidExpenses,
                unpaidAmount = source.Summary.UnpaidExpenses,
                withCardAmount = source.Summary.ExpensesWithCard,
                withoutCardAmount = source.Summary.ExpensesWithoutCard,
                paidCount = source.Summary.PaidCount,
                unpaidCount = source.Summary.UnpaidCount,
                // Card aggregates
                totalCardStatements = source.CreditCards.Sum(c => c.StatementAmount ?? 0m),
                totalCardPaid = source.CreditCards.Where(c => c.InvoicePaid == true).Sum(c => c.StatementAmount ?? 0m),
                totalCardPending = source.CreditCards.Where(c => c.InvoicePaid != true).Sum(c => c.StatementAmount ?? 0m),
                cardsPaidCount = source.CreditCards.Count(c => c.InvoicePaid == true),
                cardsPendingCount = source.CreditCards.Count(c => c.InvoicePaid != true && c.StatementAmount > 0),
                // What really needs to be paid (debit expenses pending + card invoices pending)
                totalToPay = source.Summary.UnpaidExpenses + source.CreditCards.Where(c => c.InvoicePaid != true).Sum(c => c.StatementAmount ?? 0m),
                // What can be invested (income - all expenses - all card statements)
                availableToInvest = source.Summary.TotalIncome - source.Summary.TotalExpenses - source.CreditCards.Sum(c => c.StatementAmount ?? 0m)
            },
            incomes = source.Incomes.Select(item => new
            {
                id = item.Id,
                name = item.Description,
                amount = item.Amount,
                dayOfMonth = item.Date.Day,
                isRecurring = item.IsRecurring,
                isPaid = item.Status == TransactionStatus.Paid,
                paymentDate = item.PaidDate,
                details = item.Details,
                createdAt = item.CreatedAt,
                updatedAt = item.UpdatedAt
            }),
            expenses = source.Expenses.Where(item => !item.IsSubscription).Select(item => new
            {
                id = item.Id,
                name = item.Description,
                amount = item.Amount,
                paymentType = item.PaymentMethod == PaymentMethod.CreditCard ? "credit" : "debit",
                dueDay = item.Date.Day,
                isPaid = item.Status == TransactionStatus.Paid,
                paymentDate = item.PaidDate,
                details = item.Details,
                creditCardId = item.CreditCardId,
                creditCardName = item.CreditCardName,
                hasVariableAmount = item.HasVariableAmount,
                createdAt = item.CreatedAt,
                updatedAt = item.UpdatedAt
            }),
            subscriptions = subscriptionItems,
            cardSummaries = source.CreditCards.Select(card => new
            {
                creditCardId = card.CreditCardId,
                creditCardName = card.CreditCardName,
                totalAmount = card.TotalAmount,
                paidAmount = card.PaidAmount,
                pendingAmount = card.PendingAmount,
                itemCount = card.ExpenseCount,
                statementAmount = card.StatementAmount,
                invoiceId = card.InvoiceId,
                invoicePaid = card.InvoicePaid,
                invoicePaymentDate = card.InvoicePaymentDate,
                creditLimit = card.CreditLimit,
                availableCredit = card.AvailableCredit
            }),
            invoicesDueThisMonth = source.InvoicesDueThisMonth.Select(inv => new
            {
                invoiceId = inv.InvoiceId,
                creditCardGroupId = inv.CreditCardGroupId,
                creditCardGroupName = inv.CreditCardGroupName,
                dueDate = inv.DueDate,
                referenceMonth = inv.ReferenceMonth,
                referenceYear = inv.ReferenceYear,
                totalTransactionsAmount = inv.TotalTransactionsAmount,
                statementAmount = inv.StatementAmount,
                isPaid = inv.IsPaid,
                paymentDate = inv.PaymentDate,
                transactions = inv.Transactions.Select(tx => new
                {
                    transactionId = tx.TransactionId,
                    description = tx.Description,
                    amount = tx.Amount,
                    date = tx.Date,
                    isPaid = tx.IsPaid,
                    creditCardId = tx.CreditCardId,
                    creditCardName = tx.CreditCardName
                }),
                linkedSubscriptions = inv.LinkedSubscriptions.Select(ls => new
                {
                    templateId = ls.TemplateId,
                    description = ls.Description,
                    amount = ls.Amount,
                    hasVariableAmount = ls.HasVariableAmount,
                    creditCardId = ls.CreditCardId,
                    creditCardName = ls.CreditCardName
                })
            })
        };
    }

    [HttpPost("copy-recurring/{year:int}/{month:int}")]
    public async Task<IActionResult> CopyRecurring(int year, int month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.CopyRecurringForMonthAsync(year, month, userId.Value, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { code = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpPost("parse-statement")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ParseStatement(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo inválido." });

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
            !file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Apenas arquivos PDF são suportados." });

        try
        {
            using var stream = file.OpenReadStream();
            var transactions = new List<object>();
            await foreach (var tx in _pdfParser.ParseAsync(stream, cancellationToken))
            {
                transactions.Add(new
                {
                    description = tx.RawDescription,
                    amount = tx.Amount,
                    date = tx.TransactionDate.ToString("yyyy-MM-dd")
                });
            }
            return Ok(new { transactions });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static string ToBillingFrequency(FrequencyType frequencyType) => frequencyType switch
    {
        FrequencyType.Weekly => "weekly",
        FrequencyType.Quarterly => "quarterly",
        FrequencyType.Yearly => "yearly",
        _ => "monthly"
    };

    private async Task<FinancialAccount?> GetDefaultAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accounts = await _financialAccountRepository.GetAllByUserIdAsync(userId, cancellationToken);
        return accounts.FirstOrDefault(x => x.IsActive);
    }

    private static DateTime CreateDate(int year, int month, int day)
    {
        var safeDay = Math.Max(1, Math.Min(day, DateTime.DaysInMonth(year, month)));
        return new DateTime(year, month, safeDay, 12, 0, 0, DateTimeKind.Utc);
    }

    private async Task<CreateRecurringTransactionRequest?> BuildSubscriptionTemplateRequestAsync(PersonalControlSubscriptionRequest request, Guid userId, CancellationToken cancellationToken)
    {
        Guid? accountId = null;
        Guid? cardId = null;
        var paymentMethod = request.PaymentType == "credit" ? PaymentMethod.CreditCard : PaymentMethod.DebitCard;

        if (paymentMethod == PaymentMethod.CreditCard && Guid.TryParse(request.CreditCardId, out var parsedCardId))
        {
            var card = await _creditCardRepository.GetByIdAndUserAsync(parsedCardId, userId, cancellationToken);
            if (card != null)
                cardId = parsedCardId;
        }

        // Fall back to default account (debit) if no valid card resolved
        if (!cardId.HasValue)
        {
            paymentMethod = PaymentMethod.DebitCard;
            accountId = (await GetDefaultAccountAsync(userId, cancellationToken))?.Id;
            if (!accountId.HasValue)
                return null;
        }

        // Use seeded "Não Categorizado" category to avoid validation failure
        var defaultCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000014");

        return new CreateRecurringTransactionRequest
        {
            Type = Diax.Domain.Finance.Planner.TransactionType.Expense,
            ItemKind = RecurringItemKind.Subscription,
            Description = request.Name,
            Details = request.Details,
            Amount = request.Amount,
            CategoryId = defaultCategoryId,
            FrequencyType = request.BillingFrequency switch
            {
                "weekly" => FrequencyType.Weekly,
                "quarterly" => FrequencyType.Quarterly,
                "yearly" => FrequencyType.Yearly,
                _ => FrequencyType.Monthly
            },
            DayOfMonth = 1,
            StartDate = CreateDate(request.Year, request.Month, 1),
            PaymentMethod = paymentMethod,
            CreditCardId = cardId,
            FinancialAccountId = accountId,
            Priority = 50,
            HasVariableAmount = request.HasVariableAmount
        };
    }

    private async Task<UpdateRecurringTransactionRequest?> BuildSubscriptionTemplateUpdateRequestAsync(PersonalControlSubscriptionRequest request, Guid userId, CancellationToken cancellationToken)
    {
        var create = await BuildSubscriptionTemplateRequestAsync(request, userId, cancellationToken);
        if (create == null)
            return null;

        return new UpdateRecurringTransactionRequest
        {
            Type = create.Type,
            ItemKind = create.ItemKind,
            Description = create.Description,
            Details = create.Details,
            Amount = create.Amount,
            CategoryId = create.CategoryId,
            FrequencyType = create.FrequencyType,
            DayOfMonth = create.DayOfMonth,
            StartDate = create.StartDate,
            EndDate = create.EndDate,
            PaymentMethod = create.PaymentMethod,
            CreditCardId = create.CreditCardId,
            FinancialAccountId = create.FinancialAccountId,
            Priority = create.Priority,
            IsActive = true,
            HasVariableAmount = create.HasVariableAmount
        };
    }

    public record PersonalControlIncomeRequest(
        [Range(2000, 2100)] int Year,
        [Range(1, 12)] int Month,
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [Range(typeof(decimal), "0.01", "999999999.99")] decimal Amount,
        [Range(1, 31)] int DayOfMonth,
        bool IsRecurring = true,
        bool IsPaid = true,
        DateTime? PaymentDate = null,
        [StringLength(2000)] string? Details = null);

    public record PersonalControlExpenseRequest(
        [Range(2000, 2100)] int Year,
        [Range(1, 12)] int Month,
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [Range(typeof(decimal), "0.01", "999999999.99")] decimal Amount,
        [Required, StringLength(50, MinimumLength = 1)] string PaymentType,
        [Range(1, 31)] int DueDay,
        bool IsPaid = false,
        DateTime? PaymentDate = null,
        [StringLength(2000)] string? Details = null,
        string? CreditCardId = null,
        bool HasVariableAmount = false);

    public record PersonalControlSubscriptionRequest(
        [Range(2000, 2100)] int Year,
        [Range(1, 12)] int Month,
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [Range(typeof(decimal), "0.01", "999999999.99")] decimal Amount,
        [Required, StringLength(50, MinimumLength = 1)] string BillingFrequency,
        [Required, StringLength(50, MinimumLength = 1)] string PaymentType,
        bool IsPaid = false,
        DateTime? PaymentDate = null,
        [StringLength(2000)] string? Details = null,
        string? CreditCardId = null,
        bool HasVariableAmount = false);

    public record TogglePersonalControlStatusRequest(bool IsPaid, DateTime? PaymentDate = null, int? Year = null, int? Month = null);

    public record TogglePersonalControlSubscriptionStatusRequest(
        [Range(2000, 2100)] int Year,
        [Range(1, 12)] int Month,
        bool IsPaid,
        DateTime? PaymentDate = null);
}
