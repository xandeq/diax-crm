using System;
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
/// Cobertura InMemory de ICustomerRepository.GetByExternalIdAsync (IMPT-01, plano 08-01): hit,
/// miss, trim de entrada e convivência de múltiplos Customers com ExternalId nulo (o índice único
/// filtrado `IX_Customers_ExternalId` permite múltiplos NULLs).
/// </summary>
public class CustomerRepositoryExternalIdTests
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
            .UseInMemoryDatabase($"customer-externalid-{Guid.NewGuid()}")
            .Options;
        return new DiaxDbContext(options, new FakeCurrentUserService(Guid.NewGuid()));
    }

    private static Customer SeedCustomer(DiaxDbContext db, string name, string email, string? externalId)
    {
        var c = new Customer(name, email, PersonType.Individual, LeadSource.Scraping);
        c.SetExternalId(externalId);
        db.Customers.Add(c);
        return c;
    }

    [Fact]
    public async Task GetByExternalIdAsync_ReturnsCustomer_WhenExternalIdMatches()
    {
        using var db = CreateDbContext();
        var target = SeedCustomer(db, "Empresa A", "a@empresa.com", "4242");
        SeedCustomer(db, "Empresa B", "b@empresa.com", "9999");
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = await repository.GetByExternalIdAsync("4242");

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
    }

    [Fact]
    public async Task GetByExternalIdAsync_ReturnsNull_WhenNoMatch()
    {
        using var db = CreateDbContext();
        SeedCustomer(db, "Empresa A", "a@empresa.com", "4242");
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = await repository.GetByExternalIdAsync("0000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByExternalIdAsync_TrimsInput()
    {
        using var db = CreateDbContext();
        var target = SeedCustomer(db, "Empresa A", "a@empresa.com", "4242");
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = await repository.GetByExternalIdAsync("  4242  ");

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
    }

    [Fact]
    public async Task GetByExternalIdAsync_IgnoresCustomersWithNullExternalId()
    {
        using var db = CreateDbContext();
        SeedCustomer(db, "Empresa Nula 1", "n1@empresa.com", null);
        SeedCustomer(db, "Empresa Nula 2", "n2@empresa.com", null);
        SeedCustomer(db, "Empresa Nula 3", "n3@empresa.com", null);
        var target = SeedCustomer(db, "Empresa A", "a@empresa.com", "4242");
        await db.SaveChangesAsync();

        var repository = new CustomerRepository(db);
        var result = await repository.GetByExternalIdAsync("4242");

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
    }
}
