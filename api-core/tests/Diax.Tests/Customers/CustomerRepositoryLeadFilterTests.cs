using System;
using System.Linq;
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
/// BFIL-01 / Phase 9: trava a garantia de que o backfill de score
/// (LeadScoringService.RecomputeAllAsync) nunca repontua os 1.065 leads marcados Inactive pelo
/// mark_junk_inactive.py — a exclusão vem do filtro `Status &lt; CustomerStatus.Customer` e até
/// aqui só era provada contra repositório mockado.
/// </summary>
public class CustomerRepositoryLeadFilterTests
{
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) { UserId = userId; }
        public Guid? UserId { get; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private static DiaxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase($"customer-leadfilter-{Guid.NewGuid()}")
            .Options;
        return new DiaxDbContext(options, new FakeCurrentUserService(Guid.NewGuid()));
    }

    private static Customer SeedCustomer(DiaxDbContext db, string name, string email, CustomerStatus status)
    {
        var c = new Customer(name, email, PersonType.Individual, LeadSource.Scraping);
        if (status != CustomerStatus.Lead)
        {
            c.UpdateStatus(status);
        }
        db.Customers.Add(c);
        return c;
    }

    [Fact]
    public async Task GetAllLeadsAsync_ExcludesInactiveAndChurned()
    {
        using var db = CreateDbContext();
        var lead = SeedCustomer(db, "Lead", "lead@x.com", CustomerStatus.Lead);
        var contacted = SeedCustomer(db, "Contacted", "contacted@x.com", CustomerStatus.Contacted);
        var qualified = SeedCustomer(db, "Qualified", "qualified@x.com", CustomerStatus.Qualified);
        var negotiating = SeedCustomer(db, "Negotiating", "negotiating@x.com", CustomerStatus.Negotiating);
        var customer = SeedCustomer(db, "Customer", "customer@x.com", CustomerStatus.Customer);
        var inactive = SeedCustomer(db, "Inactive", "inactive@x.com", CustomerStatus.Inactive);
        var churned = SeedCustomer(db, "Churned", "churned@x.com", CustomerStatus.Churned);
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = (await repository.GetAllLeadsAsync()).ToList();

        Assert.Equal(4, result.Count);
        var resultIds = result.Select(c => c.Id).ToList();
        Assert.DoesNotContain(inactive.Id, resultIds);
        Assert.DoesNotContain(churned.Id, resultIds);
        Assert.DoesNotContain(customer.Id, resultIds);
    }

    [Fact]
    public async Task GetAllLeadsAsync_IncludesAllFourOpenFunnelStatuses()
    {
        using var db = CreateDbContext();
        var lead = SeedCustomer(db, "Lead", "lead@x.com", CustomerStatus.Lead);
        var contacted = SeedCustomer(db, "Contacted", "contacted@x.com", CustomerStatus.Contacted);
        var qualified = SeedCustomer(db, "Qualified", "qualified@x.com", CustomerStatus.Qualified);
        var negotiating = SeedCustomer(db, "Negotiating", "negotiating@x.com", CustomerStatus.Negotiating);
        var customer = SeedCustomer(db, "Customer", "customer@x.com", CustomerStatus.Customer);
        var inactive = SeedCustomer(db, "Inactive", "inactive@x.com", CustomerStatus.Inactive);
        var churned = SeedCustomer(db, "Churned", "churned@x.com", CustomerStatus.Churned);
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = (await repository.GetAllLeadsAsync()).ToList();
        var resultIds = result.Select(c => c.Id).ToList();

        Assert.Equal(4, resultIds.Count);
        Assert.Contains(lead.Id, resultIds);
        Assert.Contains(contacted.Id, resultIds);
        Assert.Contains(qualified.Id, resultIds);
        Assert.Contains(negotiating.Id, resultIds);
    }

    [Fact]
    public async Task GetSegmentCountsAsync_IgnoresInactiveAndChurnedLeads()
    {
        using var db = CreateDbContext();
        var openHot = SeedCustomer(db, "OpenHot", "openhot@x.com", CustomerStatus.Lead);
        openHot.UpdateSegmentation(70, LeadSegment.Hot);

        var inactiveHot = SeedCustomer(db, "InactiveHot", "inactivehot@x.com", CustomerStatus.Inactive);
        inactiveHot.UpdateSegmentation(70, LeadSegment.Hot);

        var churnedWarm = SeedCustomer(db, "ChurnedWarm", "churnedwarm@x.com", CustomerStatus.Churned);
        churnedWarm.UpdateSegmentation(40, LeadSegment.Warm);

        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = await repository.GetSegmentCountsAsync();

        Assert.Equal(1, result[LeadSegment.Hot]);
        Assert.False(result.ContainsKey(LeadSegment.Warm));
    }
}
