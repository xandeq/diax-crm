using Diax.Domain.Common;
using Diax.Domain.Tasks;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure;

/// <summary>
/// whatsapp-event é anônimo (só API key): CurrentUserService.UserId é null e o filtro global de TaskItem
/// esconde tudo. A consulta de dedupe precisa ignorar o filtro e escopar pelo dono explícito.
/// </summary>
public class TaskRepositoryOpenTaskTests
{
    private sealed class AnonymousUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private static DiaxDbContext Db(string name) => new(
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase(name).Options, new AnonymousUser());

    [Fact]
    public async Task AnonymousContext_StillSeesOwnersOpenTask_ButNotOthersOrClosedOnes()
    {
        var name = $"open-task-{Guid.NewGuid()}";
        var owner = Guid.NewGuid();
        var customer = Guid.NewGuid();
        const string title = "💬 WhatsApp respondeu: Maria";
        using var db = Db(name);
        db.TaskItems.Add(new TaskItem { Title = title, UserId = owner, CustomerId = customer });
        db.TaskItems.Add(new TaskItem { Title = "💬 WhatsApp respondeu: Joao", UserId = owner, CustomerId = customer });
        var done = new TaskItem { Title = "💬 WhatsApp respondeu: Ana", UserId = owner, CustomerId = customer };
        done.Complete();
        db.TaskItems.Add(done);
        await db.SaveChangesAsync();
        var repo = new TaskRepository(db);

        Assert.True(await repo.HasOpenTaskWithTitleAsync(owner, customer, title));
        Assert.False(await repo.HasOpenTaskWithTitleAsync(Guid.NewGuid(), customer, title));
        Assert.False(await repo.HasOpenTaskWithTitleAsync(owner, Guid.NewGuid(), title));
        Assert.False(await repo.HasOpenTaskWithTitleAsync(owner, customer, "💬 WhatsApp respondeu: Ana"));
    }
}
