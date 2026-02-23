using Diax.Domain.ImageGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ImageTemplateConfiguration : IEntityTypeConfiguration<ImageTemplate>
{
    public void Configure(EntityTypeBuilder<ImageTemplate> builder)
    {
        builder.ToTable("image_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PromptTemplate).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.DefaultParametersJson).HasMaxLength(2000);
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        builder.Property(x => x.IsEnabled).IsRequired();

        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsEnabled);
    }
}
