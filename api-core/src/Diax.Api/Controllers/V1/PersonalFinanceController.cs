using Asp.Versioning;
using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly DiaxDbContext _db;

    public PersonalFinanceController(
        PersonalFinanceControlService monthService,
        TransactionService transactionService,
        RecurringTransactionService recurringService,
        IFinancialAccountRepository financialAccountRepository,
        ICreditCardRepository creditCardRepository,
        DiaxDbContext db)
    {
        _monthService = monthService;
        _transactionService = transactionService;
        _recurringService = recurringService;
        _financialAccountRepository = financialAccountRepository;
        _creditCardRepository = creditCardRepository;
        _db = db;
    }

    [HttpGet("months/{year:int}/{month:int}")]
    public async Task<IActionResult> GetMonth(int year, int month, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _monthService.GetMonthAsync(year, month, userId.Value, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var response = MapMonthView(result.Value);
        return Ok(response);
    }

    [HttpPost("incomes")]
    public async Task<IActionResult> CreateIncome([FromBody] PersonalControlIncomeRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("incomes/{id:guid}/status")]
    public async Task<IActionResult> ToggleIncomeStatus(Guid id, [FromBody] TogglePersonalControlStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(id, userId.Value, cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] PersonalControlExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
                false),
            userId.Value,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("expenses/{id:guid}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] PersonalControlExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
                false),
            userId.Value,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("expenses/{id:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _transactionService.DeleteAsync(id, userId.Value, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("expenses/{id:guid}/status")]
    public async Task<IActionResult> ToggleExpenseStatus(Guid id, [FromBody] TogglePersonalControlStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(id, userId.Value, cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] PersonalControlSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
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
                request.PaymentType == "credit" ? PaymentMethod.CreditCard : PaymentMethod.DebitCard,
                null,
                true,
                recurringRequest.FinancialAccountId,
                recurringRequest.CreditCardId,
                null,
                request.IsPaid ? TransactionStatus.Paid : TransactionStatus.Pending,
                request.PaymentDate,
                request.Details,
                true),
            userId.Value,
            cancellationToken);

        return transactionResult.IsSuccess ? Ok(recurringResult.Value.Id) : BadRequest(transactionResult.Error);
    }

    [HttpPut("subscriptions/{id:guid}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] PersonalControlSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var recurringRequest = await BuildSubscriptionTemplateUpdateRequestAsync(request, userId.Value, cancellationToken);
        if (recurringRequest == null)
            return BadRequest(new { message = "Não foi possível atualizar a assinatura sem uma conta financeira ativa ou cartão válido." });

        var result = await _recurringService.UpdateAsync(id, recurringRequest, userId.Value);
        return HandleResult(result);
    }

    [HttpDelete("subscriptions/{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var result = await _recurringService.DeleteAsync(id, userId.Value);
        return HandleResult(result);
    }

    [HttpPatch("subscriptions/{id:guid}/status")]
    public async Task<IActionResult> ToggleSubscriptionStatus(Guid id, [FromBody] TogglePersonalControlSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(_db, cancellationToken);
        if (!userId.HasValue) return Unauthorized();

        var monthView = await _monthService.GetMonthAsync(request.Year, request.Month, userId.Value, cancellationToken);
        if (!monthView.IsSuccess)
            return BadRequest(monthView.Error);

        var occurrence = monthView.Value.Subscriptions.FirstOrDefault(x => x.SourceRecurringTransactionId == id);
        if (occurrence == null)
            return NotFound(new { message = "Assinatura não encontrada para o período informado." });

        var transaction = monthView.Value.Items.FirstOrDefault(x =>
            x.IsSubscription
            && x.Description == occurrence.Description
            && x.Amount == occurrence.Amount
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
                    true),
                userId.Value,
                cancellationToken);

            if (!createResult.IsSuccess)
                return BadRequest(createResult.Error);

            var transactionResult = await _transactionService.GetByIdAsync(createResult.Value, userId.Value, cancellationToken);
            if (!transactionResult.IsSuccess)
                return NotFound(transactionResult.Error);

            return Ok();
        }

        var toggleResult = request.IsPaid
            ? await _transactionService.MarkAsPaidAsync(transaction.Id, userId.Value, request.PaymentDate, cancellationToken)
            : await _transactionService.MarkAsPendingAsync(transaction.Id, userId.Value, cancellationToken);

        return HandleResult(toggleResult);
    }

    private static object MapMonthView(PersonalFinanceMonthResponse source)
    {
        var subscriptionItems = source.Subscriptions.Select(subscription =>
        {
            var matchedTransaction = source.Items.FirstOrDefault(item =>
                item.IsSubscription
                && item.Description == subscription.Description
                && item.Amount == subscription.Amount
                && item.Date.Date == subscription.Date.Date);

            return new
            {
                id = subscription.SourceRecurringTransactionId,
                name = subscription.Description,
                amount = subscription.Amount,
                billingFrequency = ToBillingFrequency(subscription.FrequencyType),
                paymentType = subscription.PaymentMethod == PaymentMethod.CreditCard ? "credit" : "debit",
                isPaid = matchedTransaction?.Status == TransactionStatus.Paid,
                paymentDate = matchedTransaction?.PaidDate,
                details = subscription.Details,
                creditCardId = subscription.CreditCardId,
                creditCardName = source.CreditCards.FirstOrDefault(card => card.CreditCardId == subscription.CreditCardId)?.CreditCardName,
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
                unpaidCount = source.Summary.UnpaidCount
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
                itemCount = card.ExpenseCount
            })
        };
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
            Priority = 50
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
            IsActive = true
        };
    }

    public record PersonalControlIncomeRequest(
        int Year,
        int Month,
        string Name,
        decimal Amount,
        int DayOfMonth,
        bool IsRecurring = true,
        bool IsPaid = true,
        DateTime? PaymentDate = null,
        string? Details = null);

    public record PersonalControlExpenseRequest(
        int Year,
        int Month,
        string Name,
        decimal Amount,
        string PaymentType,
        int DueDay,
        bool IsPaid = false,
        DateTime? PaymentDate = null,
        string? Details = null,
        string? CreditCardId = null);

    public record PersonalControlSubscriptionRequest(
        int Year,
        int Month,
        string Name,
        decimal Amount,
        string BillingFrequency,
        string PaymentType,
        bool IsPaid = false,
        DateTime? PaymentDate = null,
        string? Details = null,
        string? CreditCardId = null);

    public record TogglePersonalControlStatusRequest(bool IsPaid, DateTime? PaymentDate = null);

    public record TogglePersonalControlSubscriptionStatusRequest(int Year, int Month, bool IsPaid, DateTime? PaymentDate = null);
}
