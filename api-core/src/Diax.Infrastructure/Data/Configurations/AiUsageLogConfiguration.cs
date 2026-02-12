using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> builder)
    {
        builder.ToTable("ai_usage_logs");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.UserId).IsRequired(false);
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.ModelId).IsRequired();
        builder.Property(x => x.TokensInput).IsRequired();
        builder.Property(x => x.TokensOutput).IsRequired();
        builder.Property(x => x.TotalTokens).IsRequired();
        builder.Property(x => x.CostEstimated)
            .IsRequired()
            .HasPrecision(18, 6); // Higher precision for micro-costs
        builder.Property(x => x.RequestType)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Foreign Keys - RESTRICT (preserve historical data)
        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Model)
            .WithMany()
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for time-series queries
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_AiUsageLogs_CreatedAt");

        builder.HasIndex(x => x.ProviderId)
            .HasDatabaseName("IX_AiUsageLogs_ProviderId");

        builder.HasIndex(x => x.ModelId)
            .HasDatabaseName("IX_AiUsageLogs_ModelId");

        // Composite indexes for covering queries
        builder.HasIndex(x => new { x.ProviderId, x.CreatedAt })
            .HasDatabaseName("IX_AiUsageLogs_ProviderId_CreatedAt");

        builder.HasIndex(x => new { x.ModelId, x.CreatedAt })
            .HasDatabaseName("IX_AiUsageLogs_ModelId_CreatedAt");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_AiUsageLogs_UserId_CreatedAt");
    }
}
