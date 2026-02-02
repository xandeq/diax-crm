using Diax.Domain.Household;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("checklist_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(i => i.Description)
            .HasMaxLength(1000);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.Priority)
            .HasConversion<int>();

        builder.Property(i => i.StoreOrLink)
            .HasMaxLength(500);

        builder.Property(i => i.IsArchived)
            .HasDefaultValue(false);

        builder.HasIndex(i => i.CategoryId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.TargetDate);
        builder.HasIndex(i => i.IsArchived);
        builder.HasIndex(i => i.UpdatedAt);
    }
}
