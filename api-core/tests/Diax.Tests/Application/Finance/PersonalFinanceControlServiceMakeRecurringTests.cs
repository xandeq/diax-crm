using Diax.Application.Finance;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests for PersonalFinanceControlService.MakeExpenseRecurringAsync — the "Tornar Recorrente"
/// button. Besides creating the template, it must materialise the upcoming months immediately:
/// the month view only lists real Transactions for standard expenses, so a template alone is
/// invisible until copy-recurring runs (the "cliquei em recorrente e nada apareceu" bug).
/// </summary>
public class PersonalFinanceControlServiceMakeRecurringTests
{
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo = new();
    private readonly Mock<ICreditCardRepository> _creditCardRepo = new();
    private readonly Mock<ICreditCardInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<ICreditCardGroupRepository> _groupRepo = new();
    private readonly Mock<IFinancialAccountRepository> _accountRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    private PersonalFinanceControlService BuildService() => new(
        _txRepo.Object,
        _recurringRepo.Object,
        _creditCardRepo.Object,
        _invoiceRepo.Object,
        _groupRepo.Object,
        _accountRepo.Object,
        _unitOfWork.Object,
        _config,
        NullLogger<PersonalFinanceControlService>.Instance);

    private static FinancialAccount NewAccount(Guid userId, decimal initialBalance = 1000m, bool isActive = true)
        => new("Conta Corrente", AccountType.Checking, initialBalance, userId, isActive);

    private static Transaction NewSourceExpense(Guid userId, Guid accountId, decimal amount = 210m, int day = 20)
        => Transaction.CreateExpense(
            description: "Faxina Regiane",
            amount: amount,
            date: new DateTime(2026, 6, day, 12, 0, 0, DateTimeKind.Utc),
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            financialAccountId: accountId,
            status: TransactionStatus.Pending);

    /// <summary>Wires the happy-path mocks: source expense found, no month materialised yet, account active.</summary>
    private (Guid userId, FinancialAccount account, Transaction source, List<Transaction> added, List<RecurringTransaction> templates)
        SetupHappyPath(decimal balance = 1000m)
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, balance);
        var source = NewSourceExpense(userId, account.Id);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var templates = new List<RecurringTransaction>();
        _recurringRepo.Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>()))
            .Callback<RecurringTransaction>(templates.Add)
            .ReturnsAsync((RecurringTransaction r) => r);

        var added = new List<Transaction>();
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added.Add(t))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        return (userId, account, source, added, templates);
    }

    [Fact]
    public async Task MakeRecurring_ExpenseNotFound_Fails()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().MakeExpenseRecurringAsync(Guid.NewGuid(), 3, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("PersonalFinance.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task MakeRecurring_AlreadyLinked_Fails()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var source = NewSourceExpense(userId, account.Id);
        source.LinkToRecurringTemplate(Guid.NewGuid(), false);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 3, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("PersonalFinance.AlreadyRecurring", result.Error.Code);
    }

    [Fact]
    public async Task MakeRecurring_FiniteMonths_MaterialisesEachUpcomingMonth()
    {
        var (userId, account, source, added, templates) = SetupHappyPath();

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 3, userId);

        Assert.True(result.IsSuccess);

        // Template created starting next month, linked to the source expense.
        var template = Assert.Single(templates);
        Assert.Equal(new DateTime(2026, 7, 1), template.StartDate);
        Assert.Equal(new DateTime(2026, 9, 1), template.EndDate); // 3 months: jul, ago, set
        Assert.Equal(template.Id, result.Value.RecurringTransactionId);
        Assert.Equal(template.Id, source.RecurringTransactionId);

        // Materialised one pending expense per month: jul/20, ago/20, set/20.
        Assert.Equal(3, result.Value.Created.Count);
        Assert.Empty(result.Value.Skipped);
        Assert.Equal(3, added.Count);
        Assert.Collection(added,
            t => Assert.Equal(new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc), t.Date),
            t => Assert.Equal(new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc), t.Date),
            t => Assert.Equal(new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc), t.Date));
        Assert.All(added, t =>
        {
            Assert.Equal(Diax.Domain.Finance.TransactionType.Expense, t.Type);
            Assert.Equal(TransactionStatus.Pending, t.Status);
            Assert.Equal(template.Id, t.RecurringTransactionId);
            Assert.Equal(210m, t.Amount);
        });

        // App semantic: cash expenses debit the account at creation (same as copy-recurring).
        Assert.Equal(1000m - 3 * 210m, account.Balance);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MakeRecurring_Indefinite_MaterialisesTwelveMonthHorizon()
    {
        var (userId, _, source, added, templates) = SetupHappyPath(balance: 10000m);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, null, userId);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(templates).EndDate);
        Assert.Equal(12, result.Value.Created.Count);
        Assert.Equal(12, added.Count);
    }

    [Fact]
    public async Task MakeRecurring_FiniteAboveHorizon_CapsMaterialisationAtTwelve()
    {
        var (userId, _, source, added, templates) = SetupHappyPath(balance: 10000m);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 24, userId);

        Assert.True(result.IsSuccess);
        // Template still spans the full 24 months (jul/2026 + 23 = jun/2028); only the materialisation is capped.
        Assert.Equal(new DateTime(2028, 6, 1), Assert.Single(templates).EndDate);
        Assert.Equal(12, result.Value.Created.Count);
        Assert.Equal(12, added.Count);
    }

    [Fact]
    public async Task MakeRecurring_MonthAlreadyMaterialised_SkipsThatMonth()
    {
        var (userId, account, source, added, _) = SetupHappyPath();

        // August already has a transaction for whatever template gets created.
        var existing = NewSourceExpense(userId, account.Id);
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(It.IsAny<Guid>(), 2026, 8, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 3, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Created.Count);   // jul + set
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("AlreadyExists", skip.SkipReason);
        Assert.Equal(existing.Id, skip.CreatedTransactionId);
        Assert.Equal(2, added.Count);
    }

    [Fact]
    public async Task MakeRecurring_CreditCardExpense_NoInvoices_CreatesTemplateButSkipsMonths()
    {
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var source = Transaction.CreateExpense(
            description: "Consórcio Porto",
            amount: 430.50m,
            date: new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: cardId,
            status: TransactionStatus.Pending);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(source.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _invoiceRepo.Setup(r => r.GetByCardAndPeriodAsync(cardId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditCardInvoice?)null);

        RecurringTransaction? template = null;
        _recurringRepo.Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>()))
            .Callback<RecurringTransaction>(t => template = t)
            .ReturnsAsync((RecurringTransaction r) => r);

        var result = await BuildService().MakeExpenseRecurringAsync(source.Id, 3, userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(template);
        Assert.Empty(result.Value.Created);
        Assert.Equal(3, result.Value.Skipped.Count);
        Assert.All(result.Value.Skipped, s => Assert.Equal("NoInvoiceFound", s.SkipReason));

        // Template must persist even when no month could be materialised.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
