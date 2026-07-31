using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class NextActionConfiguration : IEntityTypeConfiguration<NextAction>
{
    public void Configure(EntityTypeBuilder<NextAction> builder)
    {
        builder.ToTable("next_actions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Rationale)
            .HasMaxLength(1000);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.SuggestedAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TargetClass)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}
