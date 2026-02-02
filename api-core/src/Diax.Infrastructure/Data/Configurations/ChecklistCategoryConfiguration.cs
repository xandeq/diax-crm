using Diax.Domain.Household;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ChecklistCategoryConfiguration : IEntityTypeConfiguration<ChecklistCategory>
{
    public void Configure(EntityTypeBuilder<ChecklistCategory> builder)
    {
        builder.ToTable("checklist_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(c => c.Color)
            .HasMaxLength(20);

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Category)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.SortOrder);
    }
}
