using Diax.Domain.ImageGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class GeneratedImageConfiguration : IEntityTypeConfiguration<GeneratedImage>
{
    public void Configure(EntityTypeBuilder<GeneratedImage> builder)
    {
        builder.ToTable("generated_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProjectId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.ModelId).IsRequired();
        builder.Property(x => x.Prompt).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.RevisedPrompt).HasMaxLength(4000);
        builder.Property(x => x.StorageUrl).HasMaxLength(1000);
        builder.Property(x => x.ProviderUrl).HasMaxLength(1000);
        builder.Property(x => x.Width).IsRequired();
        builder.Property(x => x.Height).IsRequired();
        builder.Property(x => x.Seed).HasMaxLength(100);
        builder.Property(x => x.MetadataJson).HasMaxLength(2000);
        builder.Property(x => x.EstimatedCost).HasColumnType("decimal(10,6)");
        builder.Property(x => x.DurationMs).IsRequired();
        builder.Property(x => x.Success).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        // Relationships
        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Model)
            .WithMany()
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
