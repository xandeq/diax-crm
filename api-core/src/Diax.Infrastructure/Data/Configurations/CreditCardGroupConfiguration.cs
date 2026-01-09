using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class CreditCardGroupConfiguration : IEntityTypeConfiguration<CreditCardGroup>
{
    public void Configure(EntityTypeBuilder<CreditCardGroup> builder)
    {
        builder.ToTable("credit_card_groups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Bank)
            .HasMaxLength(200);

        builder.Property(x => x.ClosingDay)
            .IsRequired();

        builder.Property(x => x.DueDay)
            .IsRequired();

        builder.Property(x => x.SharedLimit)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        builder.HasMany(x => x.Cards)
            .WithOne(c => c.CreditCardGroup)
            .HasForeignKey(c => c.CreditCardGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Invoices)
            .WithOne(i => i.CreditCardGroup)
            .HasForeignKey(i => i.CreditCardGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_CreditCardGroups_Name");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_CreditCardGroups_IsActive");
    }
}
