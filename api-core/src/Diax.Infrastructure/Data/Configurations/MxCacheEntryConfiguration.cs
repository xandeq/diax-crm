using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

/// <summary>Configuração EF Core do cache persistente de MX por domínio (EXTR-01 / D-03).</summary>
public class MxCacheEntryConfiguration : IEntityTypeConfiguration<MxCacheEntry>
{
    public void Configure(EntityTypeBuilder<MxCacheEntry> builder)
    {
        builder.ToTable("mx_cache_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Domain)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ResultCode)
            .IsRequired();

        builder.Property(x => x.CheckedAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        // Chave lógica: um registro por domínio. TTL é avaliado em código (MxCacheEntry.IsFresh),
        // não no banco — permite TTLs diferentes por resultado (30 dias para Valid/NoMx,
        // 24h para Unverified) sem mudar o schema.
        builder.HasIndex(x => x.Domain)
            .IsUnique()
            .HasDatabaseName("IX_MxCacheEntries_Domain");

        builder.HasIndex(x => x.CheckedAt)
            .HasDatabaseName("IX_MxCacheEntries_CheckedAt");
    }
}
