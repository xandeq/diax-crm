using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AiModelConfiguration : IEntityTypeConfiguration<AiModel>
{
    public void Configure(EntityTypeBuilder<AiModel> builder)
    {
        builder.ToTable("ai_models");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);

        // Relationship
        builder.HasOne(x => x.Provider)
            .WithMany(x => x.Models)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new { x.ProviderId, x.ModelKey }).IsUnique();
        builder.HasIndex(x => new { x.ProviderId, x.IsEnabled });
    }
}
