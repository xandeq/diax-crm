using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure.Finance;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public FakeCurrentUserService(Guid userId) { UserId = userId; }
    public Guid? UserId { get; }
    public bool IsAuthenticated => UserId.HasValue;
}

/// <summary>
/// Integration test for the bdc2436 fix: ensures the repository's Include calls hydrate
/// both .Transactions and .Expenses on a CreditCardInvoice so GetTotalAmount() returns
/// the right total. EF Core's InMemory provider respects Include semantics — if a future
/// change drops .Include(i => i.Transactions), GetTotalAmount() loses the post-unification
/// data and these tests fail.
/// </summary>
public class CreditCardInvoiceRepositoryTests
{
    private static DiaxDbContext CreateDbContext(Guid userId)
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase($"invoice-repo-{Guid.NewGuid()}")
            .Options;
        return new DiaxDbContext(options, new FakeCurrentUserService(userId));
    }

    private static (CreditCardInvoice invoice, Guid userId) SeedInvoiceWithMixedItems(DiaxDbContext db, Guid userId)
    {

        // CreditCardGroup is a required relationship (non-nullable FK on the invoice),
        // so ThenInclude(g => g.Cards) will NRE on InMemory if it's missing.
        var group = new CreditCardGroup(
            name: "Itaú",
            bank: "Itaú",
            closingDay: 25,
            dueDay: 5,
            sharedLimit: 10000m,
            userId: userId);
        db.CreditCardGroups.Add(group);

        var card = new CreditCard(
            name: "Itaú Tudo Azul",
            lastFourDigits: "1234",
            limit: 5000m,
            closingDay: 25,
            dueDay: 5,
            userId: userId,
            creditCardGroupId: group.Id);
        db.CreditCards.Add(card);

        var invoice = new CreditCardInvoice(
            creditCardGroupId: group.Id,
            referenceMonth: 4,
            referenceYear: 2026,
            closingDate: new DateTime(2026, 4, 25),
            dueDate: new DateTime(2026, 5, 5),
            userId: userId);
        db.CreditCardInvoices.Add(invoice);
        var creditCardId = card.Id;

        // Two new-style Transactions linked to the invoice.
        db.Transactions.Add(Transaction.CreateExpense(
            description: "Compra A",
            amount: 200m,
            date: new DateTime(2026, 4, 10),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId,
            creditCardInvoiceId: invoice.Id));

        db.Transactions.Add(Transaction.CreateExpense(
            description: "Compra B",
            amount: 50.55m,
            date: new DateTime(2026, 4, 12),
            paymentMethod: PaymentMethod.CreditCard,
            categoryId: null,
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId,
            creditCardInvoiceId: invoice.Id));

#pragma warning disable CS0618 // Legacy Expense kept until full unification.
        db.Set<Expense>().Add(new Expense(
            description: "Compra legacy",
            amount: 49.45m,
            date: new DateTime(2026, 4, 8),
            paymentMethod: PaymentMethod.CreditCard,
            expenseCategoryId: Guid.NewGuid(),
            isRecurring: false,
            userId: userId,
            creditCardId: creditCardId,
            creditCardInvoiceId: invoice.Id));
#pragma warning restore CS0618

        db.SaveChanges();
        return (invoice, userId);
    }

    [Fact]
    public async Task GetByIdAsync_LoadsBothTransactionsAndLegacyExpenses_ForGetTotalAmount()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext(userId);
        var (invoice, _) = SeedInvoiceWithMixedItems(db, userId);
        var repo = new CreditCardInvoiceRepository(db);

        var loaded = await repo.GetByIdAsync(invoice.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Transactions.Count);
#pragma warning disable CS0618
        Assert.Single(loaded.Expenses);
#pragma warning restore CS0618
        // 200 + 50.55 (Transactions, filtered Type=Expense) + 49.45 (legacy Expense) = 300.
        Assert.Equal(300m, loaded.GetTotalAmount());
    }

    [Fact]
    public async Task GetByIdAndUserAsync_LoadsBothNavigations()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext(userId);
        var (invoice, _) = SeedInvoiceWithMixedItems(db, userId);
        var repo = new CreditCardInvoiceRepository(db);

        var loaded = await repo.GetByIdAndUserAsync(invoice.Id, userId);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Transactions.Count);
#pragma warning disable CS0618
        Assert.Single(loaded.Expenses);
#pragma warning restore CS0618
        Assert.Equal(300m, loaded.GetTotalAmount());
    }

    [Fact]
    public async Task GetByIdAndUserAsync_RejectsForeignUser()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext(userId);
        var (invoice, _) = SeedInvoiceWithMixedItems(db, userId);
        var repo = new CreditCardInvoiceRepository(db);
        var otherUserId = Guid.NewGuid();

        var loaded = await repo.GetByIdAndUserAsync(invoice.Id, otherUserId);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetUnpaidInvoicesAsync_LoadsTransactionsForGetTotalAmount()
    {
        // FinancialSummaryService.cs:90 sums GetTotalAmount() over the unpaid set;
        // before bdc2436 GetUnpaidInvoicesAsync didn't Include either nav, so the
        // pendingCredit aggregate undercounted post-unification expenses.
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext(userId);
        var (invoice, _) = SeedInvoiceWithMixedItems(db, userId);
        var repo = new CreditCardInvoiceRepository(db);

        var unpaid = await repo.GetUnpaidInvoicesAsync();

        var match = Assert.Single(unpaid, i => i.Id == invoice.Id);
        Assert.Equal(2, match.Transactions.Count);
        Assert.Equal(300m, match.GetTotalAmount());
    }

    [Fact]
    public async Task GetByGroupAndPeriodAsync_LoadsTransactionsForGetTotalAmount()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDbContext(userId);
        var (invoice, _) = SeedInvoiceWithMixedItems(db, userId);
        var repo = new CreditCardInvoiceRepository(db);

        var loaded = await repo.GetByGroupAndPeriodAsync(invoice.CreditCardGroupId, 4, 2026);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Transactions.Count);
        Assert.Equal(300m, loaded.GetTotalAmount());
    }
}
