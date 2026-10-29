using System;
using System.Linq;
using System.Threading.Tasks;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diax.Tests.Customers;

public class CustomerRepositoryNormalizationTests
{
    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }

    private static DiaxDbContext Db() => new(
        new DbContextOptionsBuilder<DiaxDbContext>().UseInMemoryDatabase($"norm-{Guid.NewGuid()}").Options,
        new FakeUser());

    private static async Task<(DiaxDbContext db, Customer[] all)> Seed()
    {
        var db = Db();
        var all = Enumerable.Range(0, 25).Select(i =>
        {
            var c = new Customer($"Cliente {i}", $"c{i}@ex.com");
            if (i % 3 != 0) c.ApplyNormalization($"Cliente {i}", "test", i % 2 == 0 ? 90 : 40, Diax.Domain.Customers.Enums.NormalizationSource.Deterministic);
            typeof(Customer).BaseType!.GetProperty("CreatedAt")!.SetValue(c, DateTime.UtcNow.AddMinutes(-(i * 7 % 25)));
            return c;
        }).ToArray();
        db.Customers.AddRange(all);
        await db.SaveChangesAsync();
        return (db, all);
    }

    [Fact]
    public async Task Counts_MatchInMemoryComputation()
    {
        var (db, all) = await Seed();
        using var _ = db;

        var counts = await new CustomerRepository(db).GetNormalizationCountsAsync();

        Assert.Equal(all.Length, counts.Total);
        Assert.Equal(all.Count(c => c.NormalizedName != null), counts.Normalized);
        Assert.Equal(all.Count(c => c.NormalizationScore >= 80), counts.High);
        Assert.Equal(all.Count(c => c.NormalizationScore is > 0 and < 80), counts.Low);
        Assert.True(counts.High > 0 && counts.Low > 0);
    }

    [Fact]
    public async Task Counts_OnEmptyTable_AreZero()
    {
        using var db = Db();
        Assert.Equal(new NormalizationCounts(0, 0, 0, 0), await new CustomerRepository(db).GetNormalizationCountsAsync());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    [InlineData(20, 10)]
    public async Task PreviewPage_MatchesOriginalOrdering(int skip, int take)
    {
        var (db, all) = await Seed();
        using var _ = db;
        var expected = all.OrderBy(c => c.NormalizedName == null ? 0 : 1).ThenBy(c => c.CreatedAt)
            .Skip(skip).Take(take).Select(c => c.Id).ToList();

        var actual = (await new CustomerRepository(db).GetNormalizationPreviewPageAsync(skip, take)).Select(c => c.Id).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }
}
