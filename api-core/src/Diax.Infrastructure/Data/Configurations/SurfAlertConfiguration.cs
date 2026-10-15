using Diax.Domain.SurfAlerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class SurfAlertConfiguration : IEntityTypeConfiguration<SurfAlert>
{
    public void Configure(EntityTypeBuilder<SurfAlert> builder)
    {
        builder.ToTable("surf_alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.GeneratedAt).IsRequired();
        builder.Property(x => x.WeekendSaturday).IsRequired();
        builder.Property(x => x.WeekendSunday).IsRequired();
        builder.Property(x => x.HasGoodWindow).IsRequired();
        builder.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.BestWindowJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AlternativesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CalendarConflictsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.SourcesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RawPayloadJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();

        // Dedupe: mesma Idempotency-Key nunca duplica (nulo é permitido várias vezes).
        builder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("IX_surf_alerts_idempotency_key")
            .IsUnique()
            .HasFilter("[idempotency_key] IS NOT NULL");

        // Listagem recente (GetRecentAsync).
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_surf_alerts_created_at");
    }
}
