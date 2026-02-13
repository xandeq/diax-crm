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

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(x => x.ModelId)
            .HasColumnName("model_id")
            .IsRequired();

        builder.Property(x => x.FeatureType)
            .HasColumnName("feature_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InputTokens)
            .HasColumnName("input_tokens");

        builder.Property(x => x.OutputTokens)
            .HasColumnName("output_tokens");

        builder.Property(x => x.EstimatedCost)
            .HasColumnName("estimated_cost")
            .HasColumnType("decimal(10,6)");

        builder.Property(x => x.Duration)
            .HasColumnName("duration")
            .IsRequired();

        builder.Property(x => x.Success)
            .HasColumnName("success")
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(500);

        builder.Property(x => x.RequestId)
            .HasColumnName("request_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_ai_usage_logs_user_id");

        builder.HasIndex(x => x.ProviderId)
            .HasDatabaseName("ix_ai_usage_logs_provider_id");

        builder.HasIndex(x => x.ModelId)
            .HasDatabaseName("ix_ai_usage_logs_model_id");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_ai_usage_logs_created_at");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("ix_ai_usage_logs_user_created");

        // Relationships
        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Model)
            .WithMany()
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
