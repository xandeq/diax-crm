using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diax.Tests.Customers;

/// <summary>
/// Os leads "prontos" de outreach saem filtrados/ordenados/limitados no SQL. O resultado tem que ser
/// idêntico ao do critério original (lista inteira em memória + OrderByDescending(Segment).ThenBy(CreatedAt).Take).
/// Também cobre o WhatsApp: o critério antigo usava Customer.IsLead (propriedade calculada, ignorada no
/// mapeamento) dentro de um Expression, que o EF não traduz.
/// </summary>
public class CustomerRepositoryOutreachReadyTests
{
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }

    private static DiaxDbContext Db() => new(
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase($"outreach-ready-{Guid.NewGuid()}").Options,
        new FakeCurrentUserService());

    private static void Set(Customer c, string prop, object? value)
    {
        var t = typeof(Customer);
        while (t != null)
        {
            var p = t.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (p != null) { p.SetValue(c, value); return; }
            t = t.BaseType;
        }
        throw new InvalidOperationException(prop);
    }

    private static List<Customer> Seed(DiaxDbContext db, DateTime now)
    {
        var list = new List<Customer>();
        var segments = new LeadSegment?[] { LeadSegment.Hot, LeadSegment.Warm, LeadSegment.Cold, null };
        var statuses = new[] { CustomerStatus.Lead, CustomerStatus.Contacted, CustomerStatus.Negotiating, CustomerStatus.Customer, CustomerStatus.Inactive };
        var i = 0;
        foreach (var seg in segments)
        foreach (var status in statuses)
        foreach (var variant in new[] { 0, 1, 2, 3 })
        {
            var email = variant == 1 ? null : variant == 2 ? "x@placeholder.local" : $"c{i}@ex.com";
            var c = new Customer($"C{i}", email);
            if (seg.HasValue) c.UpdateSegmentation(50, seg.Value);
            if (status != CustomerStatus.Lead) c.UpdateStatus(status);
            c.UpdateContactInfo(phone: variant == 0 ? "11999990000" : null, whatsApp: variant == 3 ? "11888880000" : null);
            if (i % 5 == 0) c.OptOutEmail();
            if (i % 7 == 0) c.OptOutWhatsApp();
            Set(c, "CreatedAt", now.AddMinutes(-(i % 11) * 10 - i)); // ordem de criação embaralhada, sem empates
            if (i % 3 == 0) { Set(c, "LastEmailSentAt", now.AddDays(-30)); Set(c, "LastWhatsAppSentAt", now.AddDays(-30)); }
            if (i % 3 == 1) { Set(c, "LastEmailSentAt", now.AddDays(-1)); Set(c, "LastWhatsAppSentAt", now.AddDays(-1)); }
            list.Add(c);
            db.Customers.Add(c);
            i++;
        }
        return list;
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(1000)]
    public async Task EmailReady_MatchesOriginalInMemoryCriteria(int limit)
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        var all = Seed(db, now);
        await db.SaveChangesAsync();
        var cooldown = now.AddDays(-7);

        var expected = all
            .Where(c => c.Status == CustomerStatus.Lead && c.Segment != null && c.Email != null && c.Email != string.Empty
                        && !c.Email.Contains("@placeholder.local") && !c.EmailOptOut
                        && (c.LastEmailSentAt == null || c.LastEmailSentAt < cooldown))
            .OrderByDescending(c => (int)(c.Segment ?? LeadSegment.Cold))
            .ThenBy(c => c.CreatedAt)
            .Take(limit).Select(c => c.Id).ToList();

        var actual = (await new CustomerRepository(db).GetEmailOutreachReadyAsync(cooldown, limit)).Select(c => c.Id).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(1000)]
    public async Task WhatsAppReady_MatchesOriginalInMemoryCriteria(int limit)
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        var all = Seed(db, now);
        await db.SaveChangesAsync();
        var cooldown = now.AddDays(-3);

        var expected = all
            .Where(c => c.IsLead && c.Segment != null && (c.WhatsApp != null || c.Phone != null) && !c.WhatsAppOptOut
                        && (c.LastWhatsAppSentAt == null || c.LastWhatsAppSentAt < cooldown))
            .OrderByDescending(c => (int)(c.Segment ?? LeadSegment.Cold))
            .ThenBy(c => c.CreatedAt)
            .Take(limit).Select(c => c.Id).ToList();

        var actual = (await new CustomerRepository(db).GetWhatsAppOutreachReadyAsync(cooldown, limit)).Select(c => c.Id).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task NonPositiveLimit_ReturnsEmpty()
    {
        using var db = Db();
        var repo = new CustomerRepository(db);
        Assert.Empty(await repo.GetEmailOutreachReadyAsync(DateTime.UtcNow, 0));
        Assert.Empty(await repo.GetWhatsAppOutreachReadyAsync(DateTime.UtcNow, -1));
    }

    [Fact]
    public async Task LegacyWhatsAppPredicate_UsingIsLead_IsNotTranslatable()
    {
        using var db = Db();
        Seed(db, DateTime.UtcNow);
        await db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            new CustomerRepository(db).FindAsync(c => c.IsLead && c.Segment != null));
    }
}
