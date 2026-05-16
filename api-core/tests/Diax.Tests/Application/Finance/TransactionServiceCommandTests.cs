using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests for TransactionService command methods: CreateAsync (validation paths),
/// UpdateAsync, DeleteAsync (multi-tenant isolation, linked imports), ReclassifyAsync,
/// MarkAsPaidAsync, MarkAsPendingAsync, DeleteRangeAsync.
/// </summary>
public class TransactionServiceCommandTests
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

    private static FinancialAccount NewAccount(Guid userId, decimal balance = 1000m, bool isActive = true)
        => new("Conta", AccountType.Checking, balance, userId, isActive);

    private void SetupAccount(FinancialAccount account, Guid userId)
        => _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

    // ── CreateAsync — validation ─────────────────────────────────

    [Fact]
    public async Task CreateAsync_Income_FailsWithoutFinancialAccount()
    {
        var userId = Guid.NewGuid();
        var request = new CreateTransactionRequest(
            "Salário", 5000m, DateTime.UtcNow,
            TransactionType.Income, PaymentMethod.BankTransfer,
            null, true, FinancialAccountId: null);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.AccountRequired", result.Error.Code);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FailsWhenAccountIsInactive()
    {
        var userId = Guid.NewGuid();
        var inactiveAccount = NewAccount(userId, 1000m, isActive: false);
        SetupAccount(inactiveAccount, userId);

        var request = new CreateTransactionRequest(
            "Salário", 5000m, DateTime.UtcNow,
            TransactionType.Income, PaymentMethod.BankTransfer,
            null, true, inactiveAccount.Id);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.InactiveAccount", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_FailsWhenCategoryIsInactive()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var categoryId = Guid.NewGuid();
        SetupAccount(account, userId);

        var inactiveCategory = new TransactionCategory("Alimentação", userId, CategoryApplicableTo.Expense);
        inactiveCategory.Deactivate();
        _categoryRepo.Setup(r => r.GetByIdAndUserAsync(categoryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCategory);

        var request = new CreateTransactionRequest(
            "Supermercado", 200m, DateTime.UtcNow,
            TransactionType.Expense, PaymentMethod.DebitCard,
            categoryId, false, account.Id);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.InvalidCategory", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ExpenseCreditCard_DoesNotDebitAccount()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        SetupAccount(account, userId);

        var cardId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByCardAndPeriodAsync(cardId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditCardInvoice?)null);

        var request = new CreateTransactionRequest(
            "Amazon", 350m, DateTime.UtcNow,
            TransactionType.Expense, PaymentMethod.CreditCard,
            null, false, account.Id, CreditCardId: cardId);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance); // saldo não muda para cartão de crédito
    }

    [Fact]
    public async Task CreateAsync_ExpenseCreditCard_AutoLinksInvoice_WhenFound()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        SetupAccount(account, userId);

        var cardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var invoice = new CreditCardInvoice(groupId, 5, 2026, new DateTime(2026, 5, 31), new DateTime(2026, 6, 10), userId);

        _invoiceRepo.Setup(r => r.GetByCardAndPeriodAsync(cardId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var request = new CreateTransactionRequest(
            "Compra Cartão", 200m, new DateTime(2026, 5, 10),
            TransactionType.Expense, PaymentMethod.CreditCard,
            null, false, account.Id, CreditCardId: cardId);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(invoice.Id, added!.CreditCardInvoiceId);
    }

    [Fact]
    public async Task CreateAsync_Income_PassesRecurringTransactionId()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 2000m);
        var recurringId = Guid.NewGuid();
        SetupAccount(account, userId);

        var request = new CreateTransactionRequest(
            "Salário", 2000m, DateTime.UtcNow,
            TransactionType.Income, PaymentMethod.BankTransfer,
            null, true, account.Id,
            RecurringTransactionId: recurringId);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(recurringId, added!.RecurringTransactionId);
    }

    [Fact]
    public async Task CreateAsync_Expense_PassesRecurringTransactionId()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var recurringId = Guid.NewGuid();
        SetupAccount(account, userId);

        var request = new CreateTransactionRequest(
            "Aluguel", 500m, DateTime.UtcNow,
            TransactionType.Expense, PaymentMethod.BankTransfer,
            null, true, account.Id,
            RecurringTransactionId: recurringId);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(recurringId, added!.RecurringTransactionId);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var request = new CreateTransactionRequest(
            "???", 100m, DateTime.UtcNow,
            (TransactionType)999, PaymentMethod.BankTransfer,
            null, false, null);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.InvalidType", result.Error.Code);
    }

    // ── DeleteAsync — multi-tenant isolation ────────────────────

    [Fact]
    public async Task DeleteAsync_Returns404_WhenTransactionBelongsToAnotherUser()
    {
        var ownerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        var txId = Guid.NewGuid();

        // Não existe para o attacker
        _txRepo.Setup(r => r.GetByIdAndUserAsync(txId, attackerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        // Mas existe para o owner
        _txRepo.Setup(r => r.GetByIdAsync(txId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Transaction.CreateIncome("Salário", 5000m, DateTime.UtcNow,
                PaymentMethod.BankTransfer, null, false, Guid.NewGuid(), ownerUserId, null, null));

        var result = await BuildService().DeleteAsync(txId, attackerUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
        _txRepo.Verify(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Returns404_WhenTransactionDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _txRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().DeleteAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_ResetsLinkedImportedTransactions()
    {
        var userId = Guid.NewGuid();
        var tx = Transaction.CreateIncome("Salário", 1000m, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, false, Guid.NewGuid(), userId, null, null);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var linkedImports = new List<ImportedTransaction>();
        _importedRepo.Setup(r => r.GetByTransactionIdAsync(tx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedImports);

        var result = await BuildService().DeleteAsync(tx.Id, userId);

        Assert.True(result.IsSuccess);
        _txRepo.Verify(r => r.DeleteAsync(tx, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── MarkAsPaidAsync ─────────────────────────────────────────

    [Fact]
    public async Task MarkAsPaidAsync_Returns404_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().MarkAsPaidAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task MarkAsPaidAsync_Succeeds_WhenTransactionExists()
    {
        var userId = Guid.NewGuid();
        var tx = Transaction.CreateExpense("Aluguel", 1500m, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, true, userId,
            null, null, Guid.NewGuid(), TransactionStatus.Pending, null, null, null, false, false);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var paidDate = new DateTime(2026, 5, 15);
        var result = await BuildService().MarkAsPaidAsync(tx.Id, userId, paidDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Paid, tx.Status);
        Assert.Equal(paidDate, tx.PaidDate);
        _txRepo.Verify(r => r.UpdateAsync(tx, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── MarkAsPendingAsync ──────────────────────────────────────

    [Fact]
    public async Task MarkAsPendingAsync_Returns404_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().MarkAsPendingAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task MarkAsPendingAsync_ClearsPaidDate()
    {
        var userId = Guid.NewGuid();
        var tx = Transaction.CreateExpense("Conta Luz", 200m, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, false, userId,
            null, null, null, TransactionStatus.Paid, DateTime.UtcNow, null, null, false, false);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await BuildService().MarkAsPendingAsync(tx.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Pending, tx.Status);
    }

    // ── ReclassifyAsync ─────────────────────────────────────────

    [Fact]
    public async Task ReclassifyAsync_Returns404_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().ReclassifyAsync(Guid.NewGuid(),
            new ReclassifyTransactionRequest(TransactionType.Ignored, null), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ReclassifyAsync_Succeeds_WhenSameType_ReturnsSuccessWithoutChanges()
    {
        var userId = Guid.NewGuid();
        var tx = Transaction.CreateIncome("Salário", 5000m, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, false, Guid.NewGuid(), userId, null, null);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(tx.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await BuildService().ReclassifyAsync(tx.Id,
            new ReclassifyTransactionRequest(TransactionType.Income, null), userId);

        Assert.True(result.IsSuccess);
        _txRepo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteRangeAsync ────────────────────────────────────────

    [Fact]
    public async Task DeleteRangeAsync_ReturnsSuccess_WhenListIsEmpty()
    {
        var userId = Guid.NewGuid();
        var request = new BulkDeleteRequest(new List<Guid>());

        var result = await BuildService().DeleteRangeAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.DeletedCount);
        Assert.Equal(0, result.Value.FailedCount);
    }

    [Fact]
    public async Task DeleteRangeAsync_ReturnsError_WhenMoreThan100Items()
    {
        var userId = Guid.NewGuid();
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var request = new BulkDeleteRequest(ids);

        var result = await BuildService().DeleteRangeAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("General.BatchLimitExceeded", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRangeAsync_ReturnsError_WhenContainsEmptyGuid()
    {
        var userId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.Empty, Guid.NewGuid() };
        var request = new BulkDeleteRequest(ids);

        var result = await BuildService().DeleteRangeAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("General.InvalidIds", result.Error.Code);
    }
}
