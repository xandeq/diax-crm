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
