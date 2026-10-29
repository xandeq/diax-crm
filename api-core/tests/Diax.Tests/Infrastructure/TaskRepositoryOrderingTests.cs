using Diax.Domain.Common;
using Diax.Domain.Tasks;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure;

public class TaskRepositoryOrderingTests
{
    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }

    private static readonly FakeUser User = new();

    private static DiaxDbContext Db() => new(
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase($"tasks-order-{Guid.NewGuid()}").Options,
        User);

    private static TaskItem T(string title, TaskItemPriority p, DateTime? due, Guid user) =>
        new() { Title = title, Priority = p, DueDate = due, UserId = user };

    [Fact]
    public async Task SamePriority_DatedTasksComeBeforeUndated_EarliestFirst()
    {
        using var db = Db();
        var u = User.UserId!.Value;
        var now = DateTime.UtcNow;
        db.TaskItems.AddRange(
            T("sem prazo", TaskItemPriority.High, null, u),
            T("amanha", TaskItemPriority.High, now.AddDays(1), u),
            T("hoje", TaskItemPriority.High, now, u),
            T("urgente sem prazo", TaskItemPriority.Urgent, null, u));
        await db.SaveChangesAsync();
        var repo = new TaskRepository(db);

        var all = (await repo.GetByUserAsync(u)).Select(t => t.Title).ToList();
        var todo = (await repo.GetByStatusAsync(u, TaskItemStatus.Todo)).Select(t => t.Title).ToList();

        var expected = new[] { "urgente sem prazo", "hoje", "amanha", "sem prazo" };
        Assert.Equal(expected, all);
        Assert.Equal(expected, todo);
    }
}
