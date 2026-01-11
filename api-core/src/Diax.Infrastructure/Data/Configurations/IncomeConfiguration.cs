using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.ToTable("incomes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .IsRequired();

        builder.Property(x => x.IncomeCategoryId)
            .IsRequired();

        builder.Property(x => x.IsRecurring)
            .IsRequired();

        builder.Property(x => x.FinancialAccountId)
            .IsRequired();

        // IncomeCategory relationship
        builder.HasOne(x => x.IncomeCategory)
            .WithMany()
            .HasForeignKey(x => x.IncomeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // FinancialAccount relationship (defined here for clarity,
        // the inverse is in FinancialAccountConfiguration)
        builder.HasOne(x => x.FinancialAccount)
            .WithMany(a => a.Incomes)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
