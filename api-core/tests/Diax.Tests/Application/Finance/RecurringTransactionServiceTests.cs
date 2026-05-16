using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Tests.Application.Finance;

public class RecurringTransactionServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _repo = new();
    private readonly Mock<IFinancialAccountRepository> _accountRepo = new();
    private readonly Mock<ICreditCardRepository> _cardRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RecurringTransactionService Build() => new(
        _repo.Object,
        _accountRepo.Object,
        _cardRepo.Object,
        _uow.Object,
        NullLogger<RecurringTransactionService>.Instance);

    private static FinancialAccount NewAccount(Guid userId) =>
        new("Conta Corrente", AccountType.Checking, 1000m, userId, isActive: true);

    private static CreditCard NewCard(Guid userId) =>
        new("Nubank", "1234", 5000m, closingDay: 5, dueDay: 10, userId);

    private CreateRecurringTransactionRequest ValidDebitRequest(Guid accountId) => new()
    {
        Type = PlannerTransactionType.Expense,
        ItemKind = RecurringItemKind.Standard,
        Description = "Aluguel",
        Amount = 1200m,
        CategoryId = Guid.NewGuid(),
        FrequencyType = FrequencyType.Monthly,
        DayOfMonth = 5,
        StartDate = new DateTime(2026, 1, 1),
        PaymentMethod = PaymentMethod.DebitCard,
        FinancialAccountId = accountId,
        Priority = 50,
    };

    // ── Validation: description ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_EmptyDescription_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.Description = "  ";

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidDescription", result.Error.Code);
    }

    // ── Validation: amount ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ZeroAmount_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.Amount = 0m;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidAmount", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_NegativeAmount_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.Amount = -100m;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidAmount", result.Error.Code);
    }

    // ── Validation: day of month ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DayZero_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.DayOfMonth = 0;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidDay", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_Day32_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.DayOfMonth = 32;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidDay", result.Error.Code);
    }

    // ── Validation: end date before start ────────────────────────────

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.StartDate = new DateTime(2026, 6, 1);
        req.EndDate = new DateTime(2026, 1, 1);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidPeriod", result.Error.Code);
    }

    // ── Validation: credit card rules ────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreditCard_WithoutCardId_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var req = ValidDebitRequest(accountId);
        req.PaymentMethod = PaymentMethod.CreditCard;
        req.CreditCardId = null;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.CardRequired", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_CreditCard_CardNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var req = ValidDebitRequest(Guid.Empty);
        req.PaymentMethod = PaymentMethod.CreditCard;
        req.FinancialAccountId = null;
        req.CreditCardId = Guid.NewGuid();

        _cardRepo.Setup(r => r.GetByIdAndUserAsync(req.CreditCardId!.Value, userId, default))
                 .ReturnsAsync((CreditCard?)null);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.CardNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_CreditCard_WithAccountId_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var card = NewCard(userId);
        var req = ValidDebitRequest(Guid.NewGuid());
        req.PaymentMethod = PaymentMethod.CreditCard;
        req.CreditCardId = Guid.NewGuid();
        // FinancialAccountId left non-null (ValidDebitRequest sets it)

        _cardRepo.Setup(r => r.GetByIdAndUserAsync(req.CreditCardId!.Value, userId, default))
                 .ReturnsAsync(card);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidAccountLink", result.Error.Code);
    }

    // ── Validation: debit rules ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Debit_WithoutAccountId_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var req = ValidDebitRequest(Guid.Empty);
        req.FinancialAccountId = null;

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.AccountRequired", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_Debit_AccountNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var req = ValidDebitRequest(Guid.NewGuid());

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(req.FinancialAccountId!.Value, userId, default))
                    .ReturnsAsync((FinancialAccount?)null);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.AccountNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_Debit_WithCreditCardId_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var req = ValidDebitRequest(account.Id);
        req.CreditCardId = Guid.NewGuid();

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(req.FinancialAccountId!.Value, userId, default))
                    .ReturnsAsync(account);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidCardLink", result.Error.Code);
    }

    // ── Validation: subscription must be expense ──────────────────────

    [Fact]
    public async Task CreateAsync_SubscriptionAsIncome_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var req = ValidDebitRequest(account.Id);
        req.Type = PlannerTransactionType.Income;
        req.ItemKind = RecurringItemKind.Subscription;

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(req.FinancialAccountId!.Value, userId, default))
                    .ReturnsAsync(account);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.InvalidSubscription", result.Error.Code);
    }

    // ── Duplicate check ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DuplicateExists_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var req = ValidDebitRequest(account.Id);

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(req.FinancialAccountId!.Value, userId, default))
                    .ReturnsAsync(account);
        _repo.Setup(r => r.ExistsDuplicateAsync(userId, "Aluguel", 5, 1200m,
                It.IsAny<Diax.Domain.Finance.TransactionType>(), RecurringItemKind.Standard, null))
             .ReturnsAsync(true);

        var result = await Build().CreateAsync(req, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.Duplicate", result.Error.Code);
    }

    // ── Successful create ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDebitRequest_CreatesAndReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var req = ValidDebitRequest(account.Id);

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(req.FinancialAccountId!.Value, userId, default))
                    .ReturnsAsync(account);
        _repo.Setup(r => r.ExistsDuplicateAsync(userId, It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<Diax.Domain.Finance.TransactionType>(),
                It.IsAny<RecurringItemKind>(), null))
             .ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>()))
             .ReturnsAsync((RecurringTransaction t) => t);

        var result = await Build().CreateAsync(req, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aluguel", result.Value.Description);
        Assert.Equal(1200m, result.Value.Amount);
        Assert.Equal(5, result.Value.DayOfMonth);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), userId))
             .ReturnsAsync((RecurringTransaction?)null);

        var result = await Build().GetByIdAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecurringTransaction.NotFound", result.Error.Code);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), userId)).ReturnsAsync(false);

        var result = await Build().DeleteAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Found_CallsDeleteAndSaves()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(id, userId)).ReturnsAsync(true);

        var result = await Build().DeleteAsync(id, userId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.DeleteAsync(id, userId), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
