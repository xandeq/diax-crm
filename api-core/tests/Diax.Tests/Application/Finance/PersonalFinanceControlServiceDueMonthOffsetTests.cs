using Diax.Application.Finance;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// DueMonthOffset must survive the round trip expense → template → materialised occurrence:
/// a sheet expense that belongs to September but is due 01/10 (offset 1) must produce an
/// October occurrence due 01/11, not 01/10.
/// </summary>
public class PersonalFinanceControlServiceDueMonthOffsetTests
{
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo = new();
    private readonly Mock<ICreditCardRepository> _creditCardRepo = new();
    private readonly Mock<ICreditCardInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<ICreditCardGroupRepository> _groupRepo = new();
    private readonly Mock<IFinancialAccountRepository> _accountRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IImportedTransactionRepository> _importedTxRepo = new();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    private TransactionService BuildTransactionService() => new(
        _txRepo.Object,
        new Mock<ITransactionCategoryRepository>().Object,
        _accountRepo.Object,
        _importedTxRepo.Object,
        _invoiceRepo.Object,
        _unitOfWork.Object,
        NullLogger<TransactionService>.Instance);

    private PersonalFinanceControlService BuildService() => new(
        _txRepo.Object,
        BuildTransactionService(),
        _recurringRepo.Object,
        _creditCardRepo.Object,
        _invoiceRepo.Object,
        _groupRepo.Object,
        _accountRepo.Object,
        _unitOfWork.Object,
        _config,
        NullLogger<PersonalFinanceControlService>.Instance);

    private static DateTime Utc(int y, int m, int d) => new(y, m, d, 12, 0, 0, DateTimeKind.Utc);

    private static FinancialAccount NewAccount(Guid userId)
        => new("Conta Corrente", AccountType.Checking, 1000m, userId, true);

    private (Guid userId, FinancialAccount account, List<Transaction> added, List<RecurringTransaction> templates) Wire()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var templates = new List<RecurringTransaction>();
        _recurringRepo.Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>()))
            .Callback<RecurringTransaction>(templates.Add)
            .ReturnsAsync((RecurringTransaction r) => r);

        var added = new List<Transaction>();
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added.Add(t))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        return (userId, account, added, templates);
    }

    private static Transaction SheetExpense(Guid userId, Guid accountId, DateTime date, DateTime? dueDate)
        => Transaction.CreateExpense("APARTAMENTO - Aluguel", 9575.14m, date, PaymentMethod.DebitCard, null, false, userId,
            financialAccountId: accountId, status: TransactionStatus.Pending, dueDate: dueDate);

    private static RecurringTransaction Template(Guid userId, Guid accountId, int dayOfMonth, int offset, PlannerTransactionType type = PlannerTransactionType.Expense)
        => new()
        {
            UserId = userId,
            Type = type,
            ItemKind = RecurringItemKind.Standard,
            Description = type == PlannerTransactionType.Income ? "SALARIO NIURO" : "APARTAMENTO - Aluguel",
            Amount = 100m,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Monthly,
            DayOfMonth = dayOfMonth,
            StartDate = new DateTime(2026, 1, 1),
            PaymentMethod = type == PlannerTransactionType.Income ? PaymentMethod.BankTransfer : PaymentMethod.DebitCard,
            FinancialAccountId = accountId,
            IsActive = true,
            DueMonthOffset = offset,
        };

    [Fact]
    public async Task MakeRecurring_PreservesOffsetOnTemplate_AndMaterialisesShiftedDueDates()
    {
        var (userId, account, added, templates) = Wire();
        var source = SheetExpense(userId, account.Id, Utc(2026, 9, 1), Utc(2026, 10, 1));
        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 2, userId);

        Assert.True(result.IsSuccess);
        var template = Assert.Single(templates);
        Assert.Equal(1, template.DueMonthOffset);
        Assert.Equal(2, added.Count);
        Assert.Collection(added,
            t => { Assert.Equal(Utc(2026, 10, 1), t.Date); Assert.Equal(Utc(2026, 11, 1), t.DueDate); },
            t => { Assert.Equal(Utc(2026, 11, 1), t.Date); Assert.Equal(Utc(2026, 12, 1), t.DueDate); });
    }

    [Fact]
    public async Task MakeRecurring_WithoutCrossMonthDue_KeepsOffsetZeroAndNullDueDate()
    {
        var (userId, account, added, templates) = Wire();
        var source = SheetExpense(userId, account.Id, Utc(2026, 9, 26), null);
        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 1, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, Assert.Single(templates).DueMonthOffset);
        var occurrence = Assert.Single(added);
        Assert.Equal(Utc(2026, 10, 26), occurrence.Date);
        Assert.Null(occurrence.DueDate);
    }

    [Fact]
    public async Task MakeRecurring_OffsetIsClampedToThreeMonths()
    {
        var (userId, account, _, templates) = Wire();
        var source = SheetExpense(userId, account.Id, Utc(2026, 9, 1), Utc(2027, 3, 1));
        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 1, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, Assert.Single(templates).DueMonthOffset);
    }

    [Fact]
    public async Task CopyRecurring_ExpenseTemplateWithOffset_MaterialisesDueDateInFollowingMonth()
    {
        var (userId, account, added, _) = Wire();
        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 10, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { Template(userId, account.Id, 31, 1) });

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 10, userId);

        Assert.True(result.IsSuccess);
        var tx = Assert.Single(added);
        Assert.Equal(Utc(2026, 10, 31), tx.Date);
        Assert.Equal(Utc(2026, 11, 30), tx.DueDate); // dia 31 grampeado a novembro
    }

    [Fact]
    public async Task CopyRecurring_TemplateWithoutOffset_LeavesDueDateNull()
    {
        var (userId, account, added, _) = Wire();
        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 10, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { Template(userId, account.Id, 15, 0) });

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 10, userId);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(added).DueDate);
    }

    [Fact]
    public async Task CopyRecurring_IncomeTemplateWithOffset_SetsCashDateAcrossYearTurn()
    {
        var (userId, account, added, _) = Wire();
        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 12, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { Template(userId, account.Id, 7, 1, PlannerTransactionType.Income) });

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 12, userId);

        Assert.True(result.IsSuccess);
        var tx = Assert.Single(added);
        Assert.Equal(Diax.Domain.Finance.TransactionType.Income, tx.Type);
        Assert.Equal(Utc(2026, 12, 7), tx.Date);
        Assert.Equal(Utc(2027, 1, 7), tx.DueDate);
        Assert.Equal(TransactionStatus.Pending, tx.Status);
        Assert.Null(tx.PaidDate);
        Assert.Equal(1000m, account.Balance); // não credita até o dinheiro cair
    }

    [Fact]
    public async Task CopyRecurring_IncomeTemplateWithoutOffset_IsPaidAndCreditsAccount()
    {
        var (userId, account, added, _) = Wire();
        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 10, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { Template(userId, account.Id, 25, 0, PlannerTransactionType.Income) });

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 10, userId);

        Assert.True(result.IsSuccess);
        var tx = Assert.Single(added);
        Assert.Equal(TransactionStatus.Paid, tx.Status);
        Assert.Equal(Utc(2026, 10, 25), tx.PaidDate);
        Assert.Null(tx.DueDate);
        Assert.Equal(1100m, account.Balance);
    }

    [Fact]
    public void ResolveDueDate_ClampsDayToTargetMonth()
    {
        var template = new RecurringTransaction { DayOfMonth = 31, DueMonthOffset = 2 };

        Assert.Equal(Utc(2027, 2, 28), template.ResolveDueDate(2026, 12));
        Assert.Null(new RecurringTransaction { DayOfMonth = 31, DueMonthOffset = 0 }.ResolveDueDate(2026, 12));
    }
}
