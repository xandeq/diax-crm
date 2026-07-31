using Diax.Domain.Finance.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AssetValuationConfiguration : IEntityTypeConfiguration<AssetValuation>
{
    public void Configure(EntityTypeBuilder<AssetValuation> builder)
    {
        builder.ToTable("asset_valuations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetId)
            .IsRequired();

        builder.Property(x => x.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.AsOf)
            .IsRequired();

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.AssetId, x.AsOf })
            .HasDatabaseName("IX_asset_valuations_asset_id_as_of");
    }
}
