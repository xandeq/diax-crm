using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class InvestmentOpportunityConfiguration : IEntityTypeConfiguration<InvestmentOpportunity>
{
    public void Configure(EntityTypeBuilder<InvestmentOpportunity> builder)
    {
        builder.ToTable("investment_opportunities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Class)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Ticker)
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Thesis)
            .HasMaxLength(1000);

        builder.Property(x => x.Score)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.SuggestedAllocationPct)
            .HasPrecision(18, 2);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Risk)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.UserId, x.GeneratedAt });
    }
}
