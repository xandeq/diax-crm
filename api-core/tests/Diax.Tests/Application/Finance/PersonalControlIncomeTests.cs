using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Personal-control incomes: the controller sends CategoryId = null on update (the seeded
/// income category is not user-owned, so re-sending it used to fail with
/// Transaction.InvalidCategory), honours IsPaid on create, and carries the cash date
/// (DueDate) when the salary lands in the following month.
/// </summary>
public class PersonalControlIncomeTests
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

    private static DateTime Utc(int y, int m, int d) => new(y, m, d, 12, 0, 0, DateTimeKind.Utc);

    private FinancialAccount WireAccount(Guid userId)
    {
        var account = new FinancialAccount("Conta", AccountType.Checking, 1000m, userId, true);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        return account;
    }

    [Fact]
    public async Task UpdateIncome_WithNullCategory_SucceedsEvenWhenStoredCategoryIsInvalid()
    {
        var userId = Guid.NewGuid();
        var account = WireAccount(userId);
        var staleCategory = Guid.NewGuid();
        var income = Transaction.CreateIncome("SALARIO KPIT", 9000m, Utc(2026, 9, 25), PaymentMethod.Pix, staleCategory, true, account.Id, userId);
        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(income);
        _categoryRepo.Setup(r => r.GetByIdAndUserAsync(staleCategory, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionCategory?)null);

        // Regression: re-sending the stored category fails validation.
        var withCategory = await BuildService().UpdateAsync(income.Id, new UpdateTransactionRequest(
            "SALARIO KPIT", 8900m, Utc(2026, 9, 25), PaymentMethod.Pix, staleCategory, true, account.Id,
            null, null, TransactionStatus.Paid, null, null, false), userId);
        Assert.False(withCategory.IsSuccess);
        Assert.Equal("Transaction.InvalidCategory", withCategory.Error.Code);

        // Fix: controller passes null → no category validation, update lands.
        var result = await BuildService().UpdateAsync(income.Id, new UpdateTransactionRequest(
            "SALARIO KPIT", 8900m, Utc(2026, 9, 25), PaymentMethod.Pix, null, true, account.Id,
            null, null, TransactionStatus.Paid, null, null, false), userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(8900m, income.Amount);
        _categoryRepo.Verify(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncome_PendingStatus_IsRespected()
    {
        var userId = Guid.NewGuid();
        var account = WireAccount(userId);
        Transaction? created = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => created = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CreateAsync(new CreateTransactionRequest(
            "SALARIO NIURO", 18158.35m, Utc(2026, 9, 1), TransactionType.Income, PaymentMethod.Pix, null, true,
            account.Id, Status: TransactionStatus.Pending, DueDate: Utc(2026, 10, 1)), userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Equal(TransactionStatus.Pending, created!.Status);
        Assert.Null(created.PaidDate);
        Assert.Equal(Utc(2026, 10, 1), created.DueDate);
    }

    [Fact]
    public async Task CreateIncome_DefaultsToPaidWithPaidDate()
    {
        var userId = Guid.NewGuid();
        var account = WireAccount(userId);
        Transaction? created = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => created = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CreateAsync(new CreateTransactionRequest(
            "SALARIO KPIT", 8900m, Utc(2026, 9, 25), TransactionType.Income, PaymentMethod.Pix, null, true,
            account.Id, Status: TransactionStatus.Paid, PaidDate: Utc(2026, 9, 25)), userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Paid, created!.Status);
        Assert.Equal(Utc(2026, 9, 25), created.PaidDate);
        Assert.Null(created.DueDate);
    }

    [Fact]
    public async Task UpdateIncome_SetsAndClearsCashDate()
    {
        var userId = Guid.NewGuid();
        var account = WireAccount(userId);
        var income = Transaction.CreateIncome("SALARIO NORTAL", 20752.40m, Utc(2026, 9, 7), PaymentMethod.Pix, null, true, account.Id, userId);
        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var set = await BuildService().UpdateAsync(income.Id, new UpdateTransactionRequest(
            "SALARIO NORTAL", 20752.40m, Utc(2026, 9, 7), PaymentMethod.Pix, null, true, account.Id,
            null, null, TransactionStatus.Pending, null, null, false, DueDate: Utc(2026, 10, 7)), userId);
        Assert.True(set.IsSuccess);
        Assert.Equal(Utc(2026, 10, 7), income.DueDate);
        Assert.Equal(TransactionStatus.Pending, income.Status);

        var clear = await BuildService().UpdateAsync(income.Id, new UpdateTransactionRequest(
            "SALARIO NORTAL", 20752.40m, Utc(2026, 9, 7), PaymentMethod.Pix, null, true, account.Id,
            null, null, TransactionStatus.Paid, Utc(2026, 10, 7), null, false), userId);
        Assert.True(clear.IsSuccess);
        Assert.Null(income.DueDate);
        Assert.Equal(TransactionStatus.Paid, income.Status);
        Assert.Equal(Utc(2026, 10, 7), income.PaidDate);

        var pending = await BuildService().UpdateAsync(income.Id, new UpdateTransactionRequest(
            "SALARIO NORTAL", 20752.40m, Utc(2026, 9, 7), PaymentMethod.Pix, null, true, account.Id,
            null, null, TransactionStatus.Pending, null, null, false), userId);
        Assert.True(pending.IsSuccess);
        Assert.Null(income.PaidDate); // voltar a pendente limpa a data de pagamento
    }
}
