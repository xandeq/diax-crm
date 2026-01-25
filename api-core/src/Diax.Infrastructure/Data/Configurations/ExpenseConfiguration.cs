using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

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

        builder.Property(x => x.ExpenseCategoryId)
            .IsRequired();

        builder.Property(x => x.IsRecurring)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        // ExpenseCategory relationship
        builder.HasOne(x => x.ExpenseCategory)
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // CreditCard relationship
        builder.HasOne(x => x.CreditCard)
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // CreditCardInvoice relationship
        builder.HasOne(x => x.CreditCardInvoice)
            .WithMany(i => i.Expenses)
            .HasForeignKey(x => x.CreditCardInvoiceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // FinancialAccount relationship
        builder.HasOne(x => x.FinancialAccount)
            .WithMany(a => a.Expenses)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
