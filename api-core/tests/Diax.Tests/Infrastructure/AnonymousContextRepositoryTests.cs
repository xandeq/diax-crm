using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Tasks;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure;

/// <summary>
/// Endpoints [AllowAnonymous] por API key (cash-flow-projection do InvestIQ, whatsapp-event) e workers em
/// background rodam sem usuário corrente (ICurrentUserService.UserId = null). O filtro global das entidades
/// IUserOwnedEntity devolve ZERO linhas nesse contexto. Todo método que já recebe o userId explícito precisa
/// ignorar o filtro e escopar pelo parâmetro.
/// </summary>
public class AnonymousContextRepositoryTests
{
    private sealed class NoUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static DiaxDbContext Db() => new(
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase($"anon-{Guid.NewGuid()}").Options, new NoUser());

    [Fact]
    public async Task FinancialAccounts_ByUser_AreVisibleWithoutCurrentUser()
    {
        using var db = Db();
        db.FinancialAccounts.Add(new FinancialAccount("Conta", AccountType.Checking, 1000m, Owner));
        db.FinancialAccounts.Add(new FinancialAccount("Outra", AccountType.Checking, 5m, Stranger));
        await db.SaveChangesAsync();
        var repo = new FinancialAccountRepository(db);

        var all = (await repo.GetAllByUserIdAsync(Owner)).ToList();

        Assert.Single(all);
        Assert.Equal(1000m, all[0].Balance);
        Assert.NotNull(await repo.GetByIdAndUserAsync(all[0].Id, Owner));
        Assert.Null(await repo.GetByIdAndUserAsync(all[0].Id, Stranger));
    }

    [Fact]
    public async Task CreditCards_ByUser_AreVisibleWithoutCurrentUser()
    {
        using var db = Db();
        db.CreditCards.Add(new CreditCard("Nubank", "1234", 5000m, 5, 12, Owner));
        db.CreditCards.Add(new CreditCard("Outro", "9999", 100m, 5, 12, Stranger));
        await db.SaveChangesAsync();
        var repo = new CreditCardRepository(db);

        var all = (await repo.GetAllByUserIdAsync(Owner)).ToList();

        Assert.Single(all);
        Assert.NotNull(await repo.GetByIdAndUserAsync(all[0].Id, Owner));
    }

    [Fact]
    public async Task CreditCardInvoices_ByUser_AreVisibleWithoutCurrentUser()
    {
        using var db = Db();
        var group = new CreditCardGroup("Grupo", "Banco", 5, 12, 5000m, Owner);
        db.CreditCardGroups.Add(group);
        db.CreditCardInvoices.Add(new CreditCardInvoice(group.Id, 11, 2026, new DateTime(2026, 11, 5), new DateTime(2026, 11, 12), Owner));
        await db.SaveChangesAsync();
        var repo = new CreditCardInvoiceRepository(db);

        Assert.Single(await repo.GetAllByUserIdAsync(Owner));
        Assert.Single(await repo.GetByReferencePeriodAsync(Owner, 11, 2026));
        Assert.Single(await repo.GetByDueDateRangeAsync(Owner, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30)));
        Assert.Empty(await repo.GetByReferencePeriodAsync(Stranger, 11, 2026));
    }

    [Fact]
    public async Task Tasks_ByUser_AndOpenTaskLookup_AreVisibleWithoutCurrentUser()
    {
        using var db = Db();
        var customer = Guid.NewGuid();
        db.TaskItems.Add(new TaskItem { Title = "Follow-up", UserId = Owner, CustomerId = customer, DueDate = DateTime.UtcNow });
        db.TaskItems.Add(new TaskItem { Title = "De outro", UserId = Stranger });
        await db.SaveChangesAsync();
        var repo = new TaskRepository(db);

        var mine = (await repo.GetByUserAsync(Owner)).ToList();

        Assert.Single(mine);
        Assert.Contains(customer, await repo.GetCustomerIdsWithOpenTasksAsync(new[] { customer }));
    }
}
