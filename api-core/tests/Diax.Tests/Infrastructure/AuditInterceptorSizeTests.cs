using Diax.Domain.Customers;
using Diax.Domain.Logs;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Diax.Tests.Infrastructure;

public class AuditInterceptorSizeTests
{
    private static DiaxDbContext NewContext() => new(
        new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase("audit-size-" + Guid.NewGuid())
            .AddInterceptors(new AuditSaveChangesInterceptor(null, NullLogger<AuditSaveChangesInterceptor>.Instance))
            .Options);

    [Fact]
    public async Task Update_StoresOnlyChangedProperties()
    {
        await using var db = NewContext();
        var customer = new Customer("Padaria Teste", "contato@padaria.test");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        customer.UpdateNotes("nota nova");
        await db.SaveChangesAsync();

        var update = await db.AuditLogs.SingleAsync(a => a.ResourceType == "Customer" && a.ChangedProperties != null);
        Assert.Contains("\"Notes\"", update.NewValues);
        Assert.DoesNotContain("padaria.test", update.NewValues); // e-mail não mudou → fora do diff
        Assert.DoesNotContain("padaria.test", update.OldValues ?? "");
    }

    [Fact]
    public async Task RepositoryUpdate_OnTrackedEntity_StoresOnlyChangedProperties()
    {
        // 21/10: Repository.UpdateAsync chamava DbSet.Update mesmo com a entidade já rastreada →
        // TODAS as colunas Modified → audit gravava o corpo inteiro (HtmlBody) em todo update da fila.
        await using var db = NewContext();
        var customer = new Customer("Padaria Repo", "contato@repo.test");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var repo = new Repository<Customer>(db);
        var tracked = await repo.GetByIdAsync(customer.Id);
        tracked!.UpdateNotes("nota via repo");
        await repo.UpdateAsync(tracked);
        await db.SaveChangesAsync();

        var update = await db.AuditLogs.SingleAsync(a => a.ResourceType == "Customer" && a.ChangedProperties != null);
        Assert.Contains("\"Notes\"", update.NewValues);
        Assert.DoesNotContain("repo.test", update.NewValues);
    }

    [Fact]
    public async Task RepositoryUpdate_OnDetachedEntity_StillPersists()
    {
        var name = "audit-detached-" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase(name).Options;
        var customer = new Customer("Loja Detached", "a@detached.test");
        await using (var db1 = new DiaxDbContext(options)) { db1.Customers.Add(customer); await db1.SaveChangesAsync(); }

        customer.UpdateNotes("editado fora do contexto");
        await using (var db2 = new DiaxDbContext(options))
        {
            await new Repository<Customer>(db2).UpdateAsync(customer);
            await db2.SaveChangesAsync();
        }

        await using var db3 = new DiaxDbContext(options);
        Assert.Equal("editado fora do contexto", (await db3.Customers.SingleAsync(c => c.Id == customer.Id)).Notes);
    }

    [Fact]
    public async Task LongStrings_AreTruncated()
    {
        await using var db = NewContext();
        var customer = new Customer("Clínica Teste", "a@clinica.test");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        customer.UpdateNotes(new string('x', 50_000));
        await db.SaveChangesAsync();

        var update = await db.AuditLogs.SingleAsync(a => a.ResourceType == "Customer" && a.ChangedProperties != null);
        Assert.True(update.NewValues!.Length < 2000 + 200, // AuditChangeCapture.MaxValueLength (internal)
            $"new_values com {update.NewValues.Length} chars");
        Assert.Contains("chars]", update.NewValues);
    }

    [Fact]
    public async Task AppLog_IsNotAudited()
    {
        await using var db = NewContext();
        db.AppLogs.Add(AppLog.Create(Diax.Domain.Logs.LogLevel.Error, LogCategory.System, "falha qualquer"));
        await db.SaveChangesAsync();

        Assert.False(await db.AuditLogs.AnyAsync(a => a.ResourceType == nameof(AppLog)));
    }
}
