using Diax.Domain.ImageGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ImageGenerationProjectConfiguration : IEntityTypeConfiguration<ImageGenerationProject>
{
    public void Configure(EntityTypeBuilder<ImageGenerationProject> builder)
    {
        builder.ToTable("image_generation_projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.ParametersJson).HasMaxLength(4000);
        builder.Property(x => x.ReferenceImageUrl).HasMaxLength(1000);

        // Relationships
        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.GeneratedImages)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
