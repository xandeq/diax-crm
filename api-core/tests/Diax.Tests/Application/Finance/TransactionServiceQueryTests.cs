using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests for TransactionService query methods: GetAllAsync, GetByIdAsync,
/// GetByTypeAsync, GetByMonthAsync, GetByStatusAsync, GetPagedAsync.
/// Command tests (Create/Update/Delete/Reclassify) are in TransactionServiceBalanceTests.cs
/// and TransactionServiceCommandTests.cs.
/// </summary>
public class TransactionServiceQueryTests
{
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<ITransactionCategoryRepository> _categoryRepo = new();
    private readonly Mock<IFinancialAccountRepository> _accountRepo = new();
    private readonly Mock<IImportedTransactionRepository> _importedRepo = new();
    private readonly Mock<ICreditCardInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private TransactionService BuildService() => new(
        _txRepo.Object,
        _categoryRepo.Object,
        _accountRepo.Object,
        _importedRepo.Object,
        _invoiceRepo.Object,
        _unitOfWork.Object,
        NullLogger<TransactionService>.Instance);

    private static Transaction MakeIncome(Guid userId, decimal amount = 1000m)
        => Transaction.CreateIncome("Salário", amount, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, true,
            Guid.NewGuid(), userId, null, null);

    private static Transaction MakeExpense(Guid userId, decimal amount = 200m, PaymentMethod pm = PaymentMethod.DebitCard)
        => Transaction.CreateExpense("Mercado", amount, DateTime.UtcNow,
            pm, null, false, userId,
            null, null, Guid.NewGuid(), TransactionStatus.Pending, null, null, null, false, false);

    // ── GetAllAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllTransactionsForUser()
    {
        var userId = Guid.NewGuid();
        var txs = new[] { MakeIncome(userId, 5000m), MakeExpense(userId, 300m) };
        _txRepo.Setup(r => r.GetAllByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(txs);

        var result = await BuildService().GetAllAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoTransactions()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetAllByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());

        var result = await BuildService().GetAllAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsTransaction_WhenFound()
    {
        var userId = Guid.NewGuid();
        var tx = MakeIncome(userId, 1500m);
        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await BuildService().GetByIdAsync(tx.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(tx.Id, result.Value.Id);
        Assert.Equal(1500m, result.Value.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenTransactionDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().GetByIdAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenTransactionBelongsToAnotherUser()
    {
        var ownerUserId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var tx = MakeIncome(ownerUserId);

        // GetByIdAndUserAsync returns null for wrong user (repository enforces this)
        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, requestingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().GetByIdAsync(tx.Id, requestingUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    // ── GetByTypeAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByTypeAsync_ReturnsOnlyMatchingType()
    {
        var userId = Guid.NewGuid();
        var incomes = new[] { MakeIncome(userId), MakeIncome(userId) };
        _txRepo.Setup(r => r.GetByTypeAsync(TransactionType.Income, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incomes);

        var result = await BuildService().GetByTypeAsync(TransactionType.Income, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
        Assert.All(result.Value, r => Assert.Equal(TransactionType.Income, r.Type));
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsEmpty_WhenNoMatchingType()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByTypeAsync(TransactionType.Transfer, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());

        var result = await BuildService().GetByTypeAsync(TransactionType.Transfer, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── GetByMonthAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetByMonthAsync_ReturnsTransactionsForGivenMonth()
    {
        var userId = Guid.NewGuid();
        var txs = new[] { MakeIncome(userId), MakeExpense(userId) };
        _txRepo.Setup(r => r.GetByMonthAsync(2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(txs);

        var result = await BuildService().GetByMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetByMonthAsync_ReturnsEmpty_ForFutureMonth()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByMonthAsync(2027, 1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());

        var result = await BuildService().GetByMonthAsync(2027, 1, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── GetByStatusAsync ────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsPendingTransactions()
    {
        var userId = Guid.NewGuid();
        var pending = new[] { MakeExpense(userId) }; // MakeExpense creates Pending
        _txRepo.Setup(r => r.GetByStatusAsync(TransactionStatus.Pending, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        var result = await BuildService().GetByStatusAsync(TransactionStatus.Pending, userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsEmpty_WhenNoMatchingStatus()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByStatusAsync(TransactionStatus.Paid, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());

        var result = await BuildService().GetByStatusAsync(TransactionStatus.Paid, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── MapToResponse ───────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_MapsTransactionFieldsCorrectly()
    {
        var userId = Guid.NewGuid();
        var tx = MakeIncome(userId, 9_000m);
        _txRepo.Setup(r => r.GetAllByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { tx });

        var result = await BuildService().GetAllAsync(userId);

        var response = result.Value.First();
        Assert.Equal(tx.Id, response.Id);
        Assert.Equal("Salário", response.Description);
        Assert.Equal(9_000m, response.Amount);
        Assert.Equal(TransactionType.Income, response.Type);
    }

    [Fact]
    public async Task GetByIdAsync_MapsExpenseFieldsCorrectly()
    {
        var userId = Guid.NewGuid();
        var tx = MakeExpense(userId, 350m, PaymentMethod.CreditCard);
        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await BuildService().GetByIdAsync(tx.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionType.Expense, result.Value.Type);
        Assert.Equal(PaymentMethod.CreditCard, result.Value.PaymentMethod);
        Assert.Equal(350m, result.Value.Amount);
    }
}
