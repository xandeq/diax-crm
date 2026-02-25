using Diax.Domain.Outreach;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class OutreachConfigConfiguration : IEntityTypeConfiguration<OutreachConfig>
{
    public void Configure(EntityTypeBuilder<OutreachConfig> builder)
    {
        builder.ToTable("outreach_configs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        // ===== IMPORTAÇÃO =====
        builder.Property(c => c.ApifyDatasetUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.ApifyApiToken)
            .HasMaxLength(500);

        builder.Property(c => c.ImportEnabled)
            .HasDefaultValue(false);

        // ===== SEGMENTAÇÃO =====
        builder.Property(c => c.SegmentationEnabled)
            .HasDefaultValue(false);

        // ===== ENVIO =====
        builder.Property(c => c.SendEnabled)
            .HasDefaultValue(false);

        builder.Property(c => c.DailyEmailLimit)
            .HasDefaultValue(200);

        builder.Property(c => c.EmailCooldownDays)
            .HasDefaultValue(7);

        // ===== TEMPLATES =====
        builder.Property(c => c.HotTemplateSubject)
            .HasMaxLength(500);

        builder.Property(c => c.HotTemplateBody)
            .HasMaxLength(8000);

        builder.Property(c => c.WarmTemplateSubject)
            .HasMaxLength(500);

        builder.Property(c => c.WarmTemplateBody)
            .HasMaxLength(8000);

        builder.Property(c => c.ColdTemplateSubject)
            .HasMaxLength(500);

        builder.Property(c => c.ColdTemplateBody)
            .HasMaxLength(8000);

        // ===== AUDITORIA =====
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        // ===== ÍNDICES =====
        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasDatabaseName("IX_OutreachConfigs_UserId");
    }
}
