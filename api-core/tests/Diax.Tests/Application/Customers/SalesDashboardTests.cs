using Diax.Application.Customers;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Tests.Application.Customers;

public class SalesDashboardTests
{
    [Fact]
    public void BuildFunnel_ComputesConversionToNext()
    {
        var counts = new Dictionary<CustomerStatus, int>
        {
            [CustomerStatus.Lead] = 90,
            [CustomerStatus.Contacted] = 5,
            [CustomerStatus.Qualified] = 3,
            [CustomerStatus.Negotiating] = 1,
            [CustomerStatus.Customer] = 1,
        };

        var funnel = SalesDashboardService.BuildFunnel(counts);

        Assert.Equal(5, funnel.Count);
        Assert.Equal(90, funnel[0].Count);
        // adiante de Lead: 5+3+1+1=10; base 100 → 10%
        Assert.Equal(0.10m, funnel[0].ConversionToNext);
        // Clientes (último) não tem conversão
        Assert.Null(funnel[^1].ConversionToNext);
    }

    [Fact]
    public void BuildFunnel_EmptyCounts_AllZeroNullConversion()
    {
        var funnel = SalesDashboardService.BuildFunnel(new Dictionary<CustomerStatus, int>());
        Assert.All(funnel, s => Assert.Equal(0, s.Count));
        Assert.All(funnel.Take(4), s => Assert.Null(s.ConversionToNext));
    }

    [Fact]
    public void BucketByMonth_FillsEmptyMonths_AndSums()
    {
        var now = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);
        var p1 = PaidProposal(1000m, new DateTime(2026, 7, 1));
        var p2 = PaidProposal(2500m, new DateTime(2026, 7, 2));
        var p3 = PaidProposal(500m, new DateTime(2026, 5, 15));

        var buckets = SalesDashboardService.BucketByMonth(new List<Proposal> { p1, p2, p3 }, now, 6);

        Assert.Equal(6, buckets.Count);
        Assert.Equal("2026-02", buckets[0].Month);
        Assert.Equal("2026-07", buckets[^1].Month);
        Assert.Equal(3500m, buckets[^1].Total);
        Assert.Equal(2, buckets[^1].Count);
        Assert.Equal(500m, buckets.Single(b => b.Month == "2026-05").Total);
        Assert.Equal(0m, buckets.Single(b => b.Month == "2026-03").Total);
    }

    [Fact]
    public void AcceptanceRate_ComputesFromSummary()
    {
        var summary = new List<(ProposalStatus, int, decimal)>
        {
            (ProposalStatus.Draft, 3, 300m),      // ignorado
            (ProposalStatus.Sent, 6, 600m),
            (ProposalStatus.Accepted, 2, 200m),
            (ProposalStatus.Paid, 2, 200m),
            (ProposalStatus.Cancelled, 1, 100m),  // ignorado
        };
        // (2+2) / (6+2+2) = 0.4
        Assert.Equal(0.4m, SalesDashboardService.AcceptanceRate(summary));
    }

    [Fact]
    public void AcceptanceRate_NoProposals_IsZero()
    {
        Assert.Equal(0m, SalesDashboardService.AcceptanceRate(new List<(ProposalStatus, int, decimal)>()));
    }

    private static Proposal PaidProposal(decimal amount, DateTime paidAt)
    {
        var p = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "T", "D", amount, null, null);
        p.MarkPaid();
        // PaidAt é setado para agora no MarkPaid; forçamos via reflection para o teste de bucketing
        typeof(Proposal).GetProperty(nameof(Proposal.PaidAt))!
            .SetValue(p, DateTime.SpecifyKind(paidAt, DateTimeKind.Utc));
        return p;
    }
}
