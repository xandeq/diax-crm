using Diax.Application.Finance;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Tests for PersonalFinanceControlService.DeleteExpenseScopedAsync — exclusão estilo
/// Google Agenda: Single (só a instância), Following (esta e as seguintes + encerra o
/// template) e All (todas as instâncias + template).
/// </summary>
public class PersonalFinanceControlServiceDeleteScopedTests
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

    private static FinancialAccount NewAccount(Guid userId)
        => new("Conta Corrente", AccountType.Checking, 1000m, userId, isActive: true);

    private static Transaction NewExpense(Guid userId, Guid accountId, int month, int day = 10)
        => Transaction.CreateExpense(
            description: "Cartao ITAU Tudo Azul",
            amount: 150m,
            date: new DateTime(2026, month, day, 12, 0, 0, DateTimeKind.Utc),
            paymentMethod: PaymentMethod.DebitCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            financialAccountId: accountId,
            status: TransactionStatus.Pending);

    /// <summary>
    /// Monta o cenário: 3 instâncias (maio/junho/julho) vinculadas a um template ativo.
    /// Retorna as instâncias, o template e a lista de transações efetivamente excluídas.
    /// </summary>
    private (Guid userId, List<Transaction> instances, RecurringTransaction template, List<Guid> deletedIds)
        SetupRecurringChain()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var template = new RecurringTransaction
        {
            UserId = userId,
            Type = Diax.Domain.Finance.Planner.TransactionType.Expense,
            ItemKind = RecurringItemKind.Standard,
            Description = "Cartao ITAU Tudo Azul",
            Amount = 150m,
            CategoryId = Guid.NewGuid(),
            FrequencyType = FrequencyType.Monthly,
            DayOfMonth = 10,
            StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
        };

        var instances = new List<Transaction>
        {
            NewExpense(userId, account.Id, 5),
            NewExpense(userId, account.Id, 6),
            NewExpense(userId, account.Id, 7),
        };
        foreach (var tx in instances)
            tx.LinkToRecurringTemplate(template.Id, false);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid _, CancellationToken _) => instances.FirstOrDefault(t => t.Id == id));

        _txRepo.Setup(r => r.GetByRecurringTransactionAsync(template.Id, userId, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, DateTime? fromDate, CancellationToken _) =>
                instances.Where(t => !fromDate.HasValue || t.Date >= fromDate.Value).ToList());

        var deletedIds = new List<Guid>();
        _txRepo.Setup(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => deletedIds.Add(t.Id))
            .Returns(Task.CompletedTask);

        _importedTxRepo.Setup(r => r.GetByTransactionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportedTransaction>());

        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _recurringRepo.Setup(r => r.GetByIdAsync(template.Id, userId)).ReturnsAsync(template);

        return (userId, instances, template, deletedIds);
    }

    [Fact]
    public async Task DeleteScoped_Single_RemovesOnlyThatInstance_KeepsTemplate()
    {
        var (userId, instances, template, deletedIds) = SetupRecurringChain();

        var result = await BuildService().DeleteExpenseScopedAsync(instances[1].Id, ExpenseDeleteScope.Single, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.DeletedCount);
        Assert.False(result.Value.TemplateEnded);
        Assert.False(result.Value.TemplateDeleted);
        Assert.Equal(new[] { instances[1].Id }, deletedIds);
        Assert.True(template.IsActive);
        Assert.Null(template.EndDate);
        _recurringRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteScoped_Following_RemovesThisAndFuture_EndsTemplate()
    {
        var (userId, instances, template, deletedIds) = SetupRecurringChain();

        // Exclui a partir de junho → junho + julho saem, maio fica
        var result = await BuildService().DeleteExpenseScopedAsync(instances[1].Id, ExpenseDeleteScope.Following, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.DeletedCount);
        Assert.True(result.Value.TemplateEnded);
        Assert.DoesNotContain(instances[0].Id, deletedIds);
        Assert.Contains(instances[1].Id, deletedIds);
        Assert.Contains(instances[2].Id, deletedIds);
        // Template encerra no último dia de maio
        Assert.Equal(new DateTime(2026, 5, 31), template.EndDate!.Value.Date);
        Assert.True(template.IsActive);
    }

    [Fact]
    public async Task DeleteScoped_FollowingFromFirstInstance_DeactivatesTemplate()
    {
        var (userId, instances, template, _) = SetupRecurringChain();

        var result = await BuildService().DeleteExpenseScopedAsync(instances[0].Id, ExpenseDeleteScope.Following, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.DeletedCount);
        Assert.False(template.IsActive);
    }

    [Fact]
    public async Task DeleteScoped_All_RemovesEverything_DeletesTemplate()
    {
        var (userId, instances, template, deletedIds) = SetupRecurringChain();

        var result = await BuildService().DeleteExpenseScopedAsync(instances[2].Id, ExpenseDeleteScope.All, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.DeletedCount);
        Assert.True(result.Value.TemplateDeleted);
        Assert.Equal(3, deletedIds.Count);
        _recurringRepo.Verify(r => r.DeleteAsync(template.Id, userId), Times.Once);
    }

    [Fact]
    public async Task DeleteScoped_NonRecurringExpense_IgnoresScope_DeletesSingle()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId);
        var lone = NewExpense(userId, account.Id, 6);

        _txRepo.Setup(r => r.GetByIdAndUserAsync(lone.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lone);
        _importedTxRepo.Setup(r => r.GetByTransactionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportedTransaction>());
        _accountRepo.Setup(r => r.GetByIdAndUserAsync(account.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await BuildService().DeleteExpenseScopedAsync(lone.Id, ExpenseDeleteScope.All, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.DeletedCount);
        Assert.False(result.Value.TemplateDeleted);
        _txRepo.Verify(r => r.GetByRecurringTransactionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteScoped_NotFound_Fails()
    {
        var userId = Guid.NewGuid();
        _txRepo.Setup(r => r.GetByIdAndUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await BuildService().DeleteExpenseScopedAsync(Guid.NewGuid(), ExpenseDeleteScope.Single, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("PersonalFinance.NotFound", result.Error.Code);
    }
}

/// <summary>
/// Regressão do bug "Cartao ITAU Tudo Azul não salva": despesa de valor variável
/// (fatura ainda não fechada) pode ser criada com valor 0; as demais continuam exigindo > 0.
/// </summary>
public class TransactionVariableAmountTests
{
    [Fact]
    public void CreateExpense_ZeroAmount_WithVariableFlag_Succeeds()
    {
        var tx = Transaction.CreateExpense(
            "Cartao Santander AAdvantage", 0m, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            PaymentMethod.DebitCard, null, false, Guid.NewGuid(),
            financialAccountId: Guid.NewGuid(), hasVariableAmount: true);

        Assert.Equal(0m, tx.Amount);
        Assert.True(tx.HasVariableAmount);
    }

    [Fact]
    public void CreateExpense_ZeroAmount_WithoutVariableFlag_Throws()
    {
        Assert.Throws<ArgumentException>(() => Transaction.CreateExpense(
            "Mercado", 0m, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            PaymentMethod.DebitCard, null, false, Guid.NewGuid(),
            financialAccountId: Guid.NewGuid(), hasVariableAmount: false));
    }

    [Fact]
    public void CreateExpense_NegativeAmount_EvenWithVariableFlag_Throws()
    {
        Assert.Throws<ArgumentException>(() => Transaction.CreateExpense(
            "Mercado", -10m, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            PaymentMethod.DebitCard, null, false, Guid.NewGuid(),
            financialAccountId: Guid.NewGuid(), hasVariableAmount: true));
    }

    [Fact]
    public void UpdateExpense_ZeroAmount_WithVariableFlag_Succeeds()
    {
        var tx = Transaction.CreateExpense(
            "Cartao ITAU Tudo Azul", 100m, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            PaymentMethod.DebitCard, null, false, Guid.NewGuid(),
            financialAccountId: Guid.NewGuid(), hasVariableAmount: true);

        tx.Update(
            tx.Description, 0m, tx.Date, tx.PaymentMethod, tx.CategoryId, tx.IsRecurring,
            tx.FinancialAccountId, tx.CreditCardId, tx.CreditCardInvoiceId,
            tx.Status, null, tx.Details, tx.IsSubscription, hasVariableAmount: true);

        Assert.Equal(0m, tx.Amount);
    }
}
