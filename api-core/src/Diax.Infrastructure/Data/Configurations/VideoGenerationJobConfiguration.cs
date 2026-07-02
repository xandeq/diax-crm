using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class VideoGenerationJobConfiguration : IEntityTypeConfiguration<VideoGenerationJob>
{
    public void Configure(EntityTypeBuilder<VideoGenerationJob> builder)
    {
        builder.ToTable("video_generation_jobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(x => x.Provider).HasColumnName("provider").IsRequired().HasMaxLength(50);
        builder.Property(x => x.Model).HasColumnName("model").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Prompt).HasColumnName("prompt").HasMaxLength(4000);
        builder.Property(x => x.NegativePrompt).HasColumnName("negative_prompt").HasMaxLength(2000);
        builder.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
        builder.Property(x => x.Width).HasColumnName("width").IsRequired();
        builder.Property(x => x.Height).HasColumnName("height").IsRequired();
        builder.Property(x => x.AspectRatio).HasColumnName("aspect_ratio").HasMaxLength(10);
        builder.Property(x => x.Seed).HasColumnName("seed").HasMaxLength(100);
        // Base64 de imagem de referência pode ter MBs — nvarchar(max); limpo ao concluir/falhar.
        builder.Property(x => x.ReferenceImageBase64).HasColumnName("reference_image_base64");
        builder.Property(x => x.AllowFallback).HasColumnName("allow_fallback").IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        builder.Property(x => x.ProviderUsed).HasColumnName("provider_used").HasMaxLength(50);
        builder.Property(x => x.ModelUsed).HasColumnName("model_used").HasMaxLength(200);
        builder.Property(x => x.VideoUrl).HasColumnName("video_url").HasMaxLength(2000);
        builder.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(2000);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        builder.Property(x => x.ErrorCategory).HasColumnName("error_category").HasMaxLength(50);
        builder.Property(x => x.FallbackOccurred).HasColumnName("fallback_occurred").IsRequired();
        builder.Property(x => x.AttemptedProvidersJson).HasColumnName("attempted_providers_json").HasMaxLength(500);
        builder.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(50);
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        // Índices para a fila (worker) e para a listagem por usuário
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
