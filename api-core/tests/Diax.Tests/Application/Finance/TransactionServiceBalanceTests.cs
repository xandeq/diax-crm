using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests focused on TransactionService side-effects to FinancialAccount.Balance via
/// ApplyBalanceImpact / ReverseBalanceImpact. These methods are private static so we
/// exercise them through CreateAsync / UpdateAsync / DeleteAsync / ReclassifyAsync.
/// </summary>
public class TransactionServiceBalanceTests
{
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<ITransactionCategoryRepository> _categoryRepo = new();
    private readonly Mock<IFinancialAccountRepository> _accountRepo = new();
    private readonly Mock<IImportedTransactionRepository> _importedRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private TransactionService BuildService() => new(
        _txRepo.Object,
        _categoryRepo.Object,
        _accountRepo.Object,
        _importedRepo.Object,
        _unitOfWork.Object,
        NullLogger<TransactionService>.Instance);

    private void SetupAccount(FinancialAccount account, Guid userId)
    {
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
    }

    private static FinancialAccount NewAccount(Guid userId, decimal initialBalance = 1000m)
        => new("Conta Corrente", AccountType.Checking, initialBalance, userId);

    [Fact]
    public async Task CreateAsync_Income_CreditsAccountBalance()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        SetupAccount(account, userId);

        var request = new CreateTransactionRequest(
            Description: "Salário",
            Amount: 5000m,
            Date: new DateTime(2026, 4, 1),
            Type: TransactionType.Income,
            PaymentMethod: PaymentMethod.BankTransfer,
            CategoryId: null,
            IsRecurring: true,
            FinancialAccountId: account.Id);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(6000m, account.Balance);
        _accountRepo.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExpenseDebitCard_DebitsAccountBalance()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        SetupAccount(account, userId);

        var request = new CreateTransactionRequest(
            Description: "Mercado",
            Amount: 250m,
            Date: new DateTime(2026, 4, 5),
            Type: TransactionType.Expense,
            PaymentMethod: PaymentMethod.DebitCard,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: account.Id);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(750m, account.Balance);
    }

    [Fact]
    public async Task CreateAsync_ExpenseCreditCard_DoesNotImpactAccountBalance()
    {
        // Credit card expenses defer balance impact until invoice payment.
        // The call still succeeds and creates the transaction; the account
        // (not even passed via FinancialAccountId) stays untouched.
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();

        var request = new CreateTransactionRequest(
            Description: "Compra cartão",
            Amount: 300m,
            Date: new DateTime(2026, 4, 10),
            Type: TransactionType.Expense,
            PaymentMethod: PaymentMethod.CreditCard,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: null,
            CreditCardId: creditCardId,
            CreditCardInvoiceId: Guid.NewGuid());

        var result = await BuildService().CreateAsync(request, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        _accountRepo.Verify(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        _txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Income_FailsWhenAccountInactive()
    {
        var userId = Guid.NewGuid();
        var account = new FinancialAccount("Conta Corrente", AccountType.Checking, 1000m, userId, isActive: false);
        SetupAccount(account, userId);

        var request = new CreateTransactionRequest(
            Description: "Salário",
            Amount: 5000m,
            Date: DateTime.UtcNow,
            Type: TransactionType.Income,
            PaymentMethod: PaymentMethod.BankTransfer,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: account.Id);

        var result = await BuildService().CreateAsync(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.InactiveAccount", result.Error.Code);
        Assert.Equal(1000m, account.Balance); // unchanged
    }

    [Fact]
    public async Task CreateAsync_Income_FailsWhenFinancialAccountIdMissing()
    {
        var request = new CreateTransactionRequest(
            Description: "Salário",
            Amount: 5000m,
            Date: DateTime.UtcNow,
            Type: TransactionType.Income,
            PaymentMethod: PaymentMethod.BankTransfer,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: null);

        var result = await BuildService().CreateAsync(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.AccountRequired", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_Income_ReversesBalanceCredit()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 6000m); // already credited
        SetupAccount(account, userId);

        var income = Transaction.CreateIncome(
            description: "Salário",
            amount: 5000m,
            date: new DateTime(2026, 4, 1),
            paymentMethod: PaymentMethod.BankTransfer,
            categoryId: null,
            isRecurring: false,
            financialAccountId: account.Id,
            userId: userId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);
        _importedRepo.Setup(r => r.GetByTransactionIdAsync(income.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportedTransaction>());

        var result = await BuildService().DeleteAsync(income.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance); // back to baseline
        _txRepo.Verify(r => r.DeleteAsync(income, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExpenseDebitCard_ReversesBalanceDebit()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 750m); // already debited
        SetupAccount(account, userId);

        var expense = Transaction.CreateExpense(
            description: "Mercado",
            amount: 250m,
            date: new DateTime(2026, 4, 5),
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            financialAccountId: account.Id);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(expense.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        _importedRepo.Setup(r => r.GetByTransactionIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportedTransaction>());

        var result = await BuildService().DeleteAsync(expense.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance); // refunded
    }

    [Fact]
    public async Task DeleteAsync_ExpenseCreditCard_DoesNotTouchAccount()
    {
        var userId = Guid.NewGuid();
        var creditCardId = Guid.NewGuid();
        var expense = Transaction.CreateExpense(
            description: "Compra cartão",
            amount: 300m,
            date: new DateTime(2026, 4, 10),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId,
            creditCardInvoiceId: Guid.NewGuid());

        _txRepo.Setup(r => r.GetByIdAndUserAsync(expense.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        _importedRepo.Setup(r => r.GetByTransactionIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportedTransaction>());

        var result = await BuildService().DeleteAsync(expense.Id, userId);

        Assert.True(result.IsSuccess);
        _accountRepo.Verify(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Returns404_WhenTransactionDoesNotBelongToUser()
    {
        // Multi-tenant isolation: tx exists but belongs to a different user.
        // Service returns NotFound (no info leak) and logs a warning.
        var requesterUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var account = NewAccount(ownerUserId);
        var foreignTx = Transaction.CreateIncome(
            "Foreign income", 100m, DateTime.UtcNow,
            PaymentMethod.BankTransfer, null, false,
            account.Id, ownerUserId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(foreignTx.Id, requesterUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _txRepo.Setup(r => r.GetByIdAsync(foreignTx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignTx);

        var result = await BuildService().DeleteAsync(foreignTx.Id, requesterUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Transaction.NotFound", result.Error.Code);
        _txRepo.Verify(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _accountRepo.Verify(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReclassifyAsync_IncomeToExpense_RevertsCreditAndAppliesDebit()
    {
        // Net effect on a 6000 balance for a 5000 transaction: -5000 (revert credit) -5000 (apply debit) = -10000.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 6000m); // already credited from the income
        SetupAccount(account, userId);

        var income = Transaction.CreateIncome(
            description: "Salário",
            amount: 5000m,
            date: new DateTime(2026, 4, 1),
            paymentMethod: PaymentMethod.DebitCard, // not credit card so reclassify-as-expense will hit the debit branch
            categoryId: null,
            isRecurring: false,
            financialAccountId: account.Id,
            userId: userId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);

        var request = new ReclassifyTransactionRequest(TransactionType.Expense);
        var result = await BuildService().ReclassifyAsync(income.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(-4000m, account.Balance); // 6000 - 5000 - 5000
    }

    [Fact]
    public async Task ReclassifyAsync_SameType_IsNoOp()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        var income = Transaction.CreateIncome(
            "x", 100m, DateTime.UtcNow, PaymentMethod.BankTransfer,
            null, false, account.Id, userId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);

        var request = new ReclassifyTransactionRequest(TransactionType.Income);
        var result = await BuildService().ReclassifyAsync(income.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance); // unchanged
        _accountRepo.Verify(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        _txRepo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangeAccount_RevertsOldAndAppliesNew()
    {
        // Income of 500 was originally on accountA (already credited). Move to accountB.
        var userId = Guid.NewGuid();
        var accountA = NewAccount(userId, 1500m); // 1000 baseline + 500 income
        var accountB = NewAccount(userId, 2000m); // baseline 2000

        var income = Transaction.CreateIncome(
            "Salário", 500m, new DateTime(2026, 4, 1),
            PaymentMethod.BankTransfer, null, false,
            accountA.Id, userId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(accountA.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA);
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(accountB.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB);

        var request = new UpdateTransactionRequest(
            Description: "Salário",
            Amount: 500m,
            Date: new DateTime(2026, 4, 1),
            PaymentMethod: PaymentMethod.BankTransfer,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: accountB.Id);

        var result = await BuildService().UpdateAsync(income.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, accountA.Balance); // reverted to baseline
        Assert.Equal(2500m, accountB.Balance); // credited
    }

    [Fact]
    public async Task UpdateAsync_DebitCardToCreditCard_RefundsAccountAndStopsTrackingThere()
    {
        // Despesa originalmente em conta corrente (DebitCard) virou despesa de cartão de
        // crédito. Esperado: a conta é creditada de volta com o valor (refund) e a nova
        // forma de pagamento (cartão) não toca em conta nenhuma.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 750m); // já debitado de 250 (1000 inicial)
        SetupAccount(account, userId);

        var expense = Transaction.CreateExpense(
            description: "Mercado",
            amount: 250m,
            date: new DateTime(2026, 4, 5),
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            financialAccountId: account.Id);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(expense.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var creditCardId = Guid.NewGuid();
        var request = new UpdateTransactionRequest(
            Description: "Mercado",
            Amount: 250m,
            Date: new DateTime(2026, 4, 5),
            PaymentMethod: PaymentMethod.CreditCard,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: null,
            CreditCardId: creditCardId);

        var result = await BuildService().UpdateAsync(expense.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance); // refunded
    }

    [Fact]
    public async Task UpdateAsync_CreditCardToDebitCard_DoesNotRefundOldButDebitsNewAccount()
    {
        // Despesa originalmente no cartão (sem impacto na conta) virou DebitCard. Esperado:
        // não há nada para reverter no lado antigo (cartão não afeta saldo) e a nova conta
        // é debitada normalmente.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1000m);
        SetupAccount(account, userId);

        var expense = Transaction.CreateExpense(
            description: "Compra",
            amount: 250m,
            date: new DateTime(2026, 4, 10),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: Guid.NewGuid(),
            creditCardInvoiceId: Guid.NewGuid());

        _txRepo.Setup(r => r.GetByIdAndUserAsync(expense.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var request = new UpdateTransactionRequest(
            Description: "Compra",
            Amount: 250m,
            Date: new DateTime(2026, 4, 10),
            PaymentMethod: PaymentMethod.DebitCard,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: account.Id,
            CreditCardId: null);

        var result = await BuildService().UpdateAsync(expense.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(750m, account.Balance); // debited; nothing to refund on the credit-card side
    }

    [Fact]
    public async Task UpdateAsync_ChangeAmount_AdjustsBalanceBy_NewMinusOld()
    {
        // Income on the same account changes from 500 → 800 → balance moves +300.
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 1500m); // 1000 baseline + 500 income
        SetupAccount(account, userId);

        var income = Transaction.CreateIncome(
            "Salário", 500m, new DateTime(2026, 4, 1),
            PaymentMethod.BankTransfer, null, false,
            account.Id, userId);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);

        var request = new UpdateTransactionRequest(
            Description: "Salário",
            Amount: 800m,
            Date: new DateTime(2026, 4, 1),
            PaymentMethod: PaymentMethod.BankTransfer,
            CategoryId: null,
            IsRecurring: false,
            FinancialAccountId: account.Id);

        var result = await BuildService().UpdateAsync(income.Id, request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1800m, account.Balance); // 1500 - 500 + 800
    }
}
