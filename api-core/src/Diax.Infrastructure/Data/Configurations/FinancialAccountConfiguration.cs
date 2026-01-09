using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AccountType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.InitialBalance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Balance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        builder.HasMany(x => x.Incomes)
            .WithOne(x => x.FinancialAccount)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Expenses)
            .WithOne(x => x.FinancialAccount)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
