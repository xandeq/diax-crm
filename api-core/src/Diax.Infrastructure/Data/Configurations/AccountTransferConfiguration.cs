using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> builder)
    {
        builder.ToTable("account_transfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.Date)
            .IsRequired();

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.FromFinancialAccountId)
            .IsRequired();

        builder.Property(t => t.ToFinancialAccountId)
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.FromFinancialAccount)
            .WithMany()
            .HasForeignKey(t => t.FromFinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToFinancialAccount)
            .WithMany()
            .HasForeignKey(t => t.ToFinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
