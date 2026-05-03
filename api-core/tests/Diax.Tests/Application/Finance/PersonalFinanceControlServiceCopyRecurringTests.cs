using Diax.Application.Finance;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests for PersonalFinanceControlService.CopyRecurringForMonthAsync — the auto-propagation
/// feature that materialises recurring templates into actual Transaction rows when the user
/// opens a new month with no entries.
/// </summary>
public class PersonalFinanceControlServiceCopyRecurringTests
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

    private static RecurringTransaction NewIncomeTemplate(Guid userId, Guid accountId, decimal amount, int dayOfMonth)
        => new()
        {
            UserId = userId,
            Type = PlannerTransactionType.Income,
            ItemKind = RecurringItemKind.Standard,
            Description = "PROSOURCEIT",
            Amount = amount,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Monthly,
            DayOfMonth = dayOfMonth,
            StartDate = new DateTime(2026, 1, 1),
            PaymentMethod = PaymentMethod.BankTransfer,
            FinancialAccountId = accountId,
            IsActive = true,
            Priority = 1
        };

    private static RecurringTransaction NewExpenseTemplate(Guid userId, Guid accountId, decimal amount, int dayOfMonth, PaymentMethod paymentMethod = PaymentMethod.DebitCard, RecurringItemKind kind = RecurringItemKind.Standard)
        => new()
        {
            UserId = userId,
            Type = PlannerTransactionType.Expense,
            ItemKind = kind,
            Description = "Aluguel",
            Amount = amount,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Monthly,
            DayOfMonth = dayOfMonth,
            StartDate = new DateTime(2026, 1, 1),
            PaymentMethod = paymentMethod,
            FinancialAccountId = paymentMethod == PaymentMethod.CreditCard ? null : accountId,
            CreditCardId = paymentMethod == PaymentMethod.CreditCard ? Guid.NewGuid() : null,
            IsActive = true,
            Priority = 1
        };

    [Fact]
    public async Task CopyRecurring_RejectsInvalidMonth()
    {
        var result = await BuildService().CopyRecurringForMonthAsync(2026, 13, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("PersonalFinance.InvalidMonth", result.Error.Code);
    }

    [Fact]
    public async Task CopyRecurring_RejectsInvalidYear()
    {
        var result = await BuildService().CopyRecurringForMonthAsync(1999, 5, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("PersonalFinance.InvalidYear", result.Error.Code);
    }

    [Fact]
    public async Task CopyRecurring_NoTemplates_ReturnsEmptyResult()
    {
        var userId = Guid.NewGuid();
        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction>());

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Created);
        Assert.Empty(result.Value.Skipped);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_IncomeTemplate_MaterialisesAndCreditsAccount()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewIncomeTemplate(userId, account.Id, 15750m, dayOfMonth: 1);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Created);
        Assert.Empty(result.Value.Skipped);
        Assert.Equal(template.Id, item.TemplateId);
        Assert.Null(item.SkipReason);
        Assert.NotNull(item.CreatedTransactionId);

        Assert.NotNull(added);
        Assert.Equal(Diax.Domain.Finance.TransactionType.Income, added!.Type);
        Assert.Equal(15750m, added.Amount);
        Assert.Equal(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc), added.Date);
        Assert.True(added.IsRecurring);
        Assert.Equal(template.Id, added.RecurringTransactionId);
        Assert.Equal(TransactionStatus.Paid, added.Status);

        Assert.Equal(16750m, account.Balance);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyRecurring_ExpenseDebitCard_MaterialisesPendingAndDebitsAccount()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewExpenseTemplate(userId, account.Id, 1500m, dayOfMonth: 5);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Created);
        Assert.NotNull(added);
        Assert.Equal(Diax.Domain.Finance.TransactionType.Expense, added!.Type);
        Assert.Equal(TransactionStatus.Pending, added.Status);
        Assert.Equal(-500m, account.Balance);
    }

    [Fact]
    public async Task CopyRecurring_VariableAmountTemplate_FlagsResultItem()
    {
        // Condomínio com taxa extra e salário dolarizado/por dias úteis: a UI usa esse flag
        // para lembrar o usuário de conferir/editar o valor depois que a transação é gerada.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewExpenseTemplate(userId, account.Id, 600m, dayOfMonth: 10);
        template.HasVariableAmount = true;
        template.Description = "Condomínio";

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Created);
        Assert.True(item.HasVariableAmount);
        Assert.NotNull(added);
        Assert.True(added!.HasVariableAmount);
    }

    [Fact]
    public async Task CopyRecurring_SubscriptionTemplate_MarksTransactionAsSubscription()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewExpenseTemplate(userId, account.Id, 49.90m, dayOfMonth: 10, kind: RecurringItemKind.Subscription);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.NotNull(added);
        Assert.True(added!.IsSubscription);
    }

    [Fact]
    public async Task CopyRecurring_AlreadyMaterialised_SkipsWithReason()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewIncomeTemplate(userId, account.Id, 5000m, 5);

        var existing = Transaction.CreateIncome(
            "PROSOURCEIT", 5000m, new DateTime(2026, 5, 5),
            PaymentMethod.BankTransfer, null, true,
            account.Id, userId, recurringTransactionId: template.Id);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Created);
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("AlreadyExists", skip.SkipReason);
        Assert.Equal(existing.Id, skip.CreatedTransactionId);

        Assert.Equal(1000m, account.Balance);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_CreditCardTemplate_NoCreditCardId_SkipsWithCreditCardSkipped()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var template = NewExpenseTemplate(userId, account.Id, 200m, 15, paymentMethod: PaymentMethod.CreditCard);
        template.CreditCardId = null; // explicitly no card

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("CreditCardSkipped", skip.SkipReason);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_CreditCardTemplate_NoInvoiceExists_SkipsWithNoInvoiceFound()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var template = NewExpenseTemplate(userId, account.Id, 44.90m, 15, paymentMethod: PaymentMethod.CreditCard);
        // CreditCardId is set by NewExpenseTemplate; invoice repo returns null by default

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _invoiceRepo.Setup(r => r.GetByCardAndPeriodAsync(template.CreditCardId!.Value, 5, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditCardInvoice?)null);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("NoInvoiceFound", skip.SkipReason);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_CreditCardTemplate_InvoiceExists_MaterialisesLinkedToInvoice()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var cardId = Guid.NewGuid();
        var template = NewExpenseTemplate(userId, account.Id, 44.90m, 15, paymentMethod: PaymentMethod.CreditCard);
        template.CreditCardId = cardId;
        template.ItemKind = RecurringItemKind.Subscription;

        var invoice = new CreditCardInvoice(
            Guid.NewGuid(), 5, 2026,
            new DateTime(2026, 5, 20), new DateTime(2026, 6, 10), userId);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _invoiceRepo.Setup(r => r.GetByCardAndPeriodAsync(cardId, 5, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Created);
        Assert.Null(item.SkipReason);
        Assert.NotNull(item.CreatedTransactionId);

        Assert.NotNull(added);
        Assert.Equal(Diax.Domain.Finance.TransactionType.Expense, added!.Type);
        Assert.Equal(44.90m, added.Amount);
        Assert.Equal(PaymentMethod.CreditCard, added.PaymentMethod);
        Assert.Equal(cardId, added.CreditCardId);
        Assert.Equal(invoice.Id, added.CreditCardInvoiceId);
        Assert.Null(added.FinancialAccountId);
        Assert.Equal(TransactionStatus.Pending, added.Status);
        Assert.True(added.IsSubscription);
        Assert.Equal(template.Id, added.RecurringTransactionId);

        // CC expenses don't touch the account balance
        Assert.Equal(1000m, account.Balance);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyRecurring_InactiveAccount_SkipsWithReason()
    {
        var userId = Guid.NewGuid();
        var inactive = NewAccount(userId, 1000m, isActive: false);
        var template = NewIncomeTemplate(userId, inactive.Id, 5000m, 5);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(inactive.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("InvalidAccount", skip.SkipReason);
        Assert.Equal(1000m, inactive.Balance);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_StartDateAfterClampedDay_SkipsWithBeforeStartDate()
    {
        // Template starts mid-month (2026-05-15) with DayOfMonth=10. The clamped
        // target date is 2026-05-10, which is BEFORE the template's StartDate.
        // RecurringTransaction.GetNextOccurrences gates this case with
        // `occurrence >= StartDate.Date`; CopyRecurring must do the same so the
        // first materialised transaction lands on or after the start date.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewExpenseTemplate(userId, account.Id, 1500m, dayOfMonth: 10);
        template.StartDate = new DateTime(2026, 5, 15);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Created);
        var skip = Assert.Single(result.Value.Skipped);
        Assert.Equal("BeforeStartDate", skip.SkipReason);
        Assert.Equal(1000m, account.Balance); // unchanged
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyRecurring_StartDateAtClampedDay_Materialises()
    {
        // Boundary: StartDate equals the target day. Should materialise.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewExpenseTemplate(userId, account.Id, 1500m, dayOfMonth: 15);
        template.StartDate = new DateTime(2026, 5, 15);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Created);
        Assert.Empty(result.Value.Skipped);
    }

    [Fact]
    public async Task CopyRecurring_DayOfMonth31_ClampsToFebruary28()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var template = NewIncomeTemplate(userId, account.Id, 5000m, dayOfMonth: 31);

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 2, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { template });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(template.Id, 2026, 2, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        Transaction? added = null;
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => added = t)
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        await BuildService().CopyRecurringForMonthAsync(2026, 2, userId);

        Assert.NotNull(added);
        Assert.Equal(new DateTime(2026, 2, 28, 12, 0, 0, DateTimeKind.Utc), added!.Date);
    }

    [Fact]
    public async Task CopyRecurring_MultipleTemplates_ProcessesEachIndependently()
    {
        // Realistic Maio/2026 scenario: 4 income templates + 1 expense template + 1 credit-card expense.
        // Income x4 should materialize, expense materializes, credit-card skips (no invoice for month).
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 0m);

        var prosource = NewIncomeTemplate(userId, account.Id, 15750m, 1);
        prosource.Description = "PROSOURCEIT";
        var kpit = NewIncomeTemplate(userId, account.Id, 9000m, 3);
        kpit.Description = "KPIT";
        var pantheon = NewIncomeTemplate(userId, account.Id, 20160m, 15);
        pantheon.Description = "PANTHEON";
        var vale = NewIncomeTemplate(userId, account.Id, 780m, 24);
        vale.Description = "VALE";
        var aluguel = NewExpenseTemplate(userId, account.Id, 1500m, 5);
        aluguel.Description = "Aluguel";
        var spotify = NewExpenseTemplate(userId, account.Id, 21.90m, 10, paymentMethod: PaymentMethod.CreditCard);
        spotify.Description = "Spotify";
        // No invoice setup → _invoiceRepo returns null by default → "NoInvoiceFound"

        _recurringRepo.Setup(r => r.GetRecurringForMonthAsync(userId, 5, 2026))
            .ReturnsAsync(new List<RecurringTransaction> { prosource, kpit, pantheon, vale, aluguel, spotify });
        _txRepo.Setup(r => r.GetByRecurringTransactionForMonthAsync(It.IsAny<Guid>(), 2026, 5, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await BuildService().CopyRecurringForMonthAsync(2026, 5, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Created.Count); // 4 incomes + 1 expense
        Assert.Single(result.Value.Skipped);          // credit-card spotify (no invoice)
        Assert.Equal("NoInvoiceFound", result.Value.Skipped[0].SkipReason);

        // 0 + 15750 + 9000 + 20160 + 780 - 1500 = 44190
        Assert.Equal(44190m, account.Balance);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
