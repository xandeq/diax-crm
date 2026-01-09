using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class CreditCardInvoiceConfiguration : IEntityTypeConfiguration<CreditCardInvoice>
{
    public void Configure(EntityTypeBuilder<CreditCardInvoice> builder)
    {
        builder.ToTable("credit_card_invoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceMonth)
            .IsRequired();

        builder.Property(x => x.ReferenceYear)
            .IsRequired();

        builder.Property(x => x.ClosingDate)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.IsPaid)
            .IsRequired();

        builder.Property(x => x.PaymentDate);

        // Relationships
        builder.HasOne(x => x.CreditCard)
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaidFromAccount)
            .WithMany()
            .HasForeignKey(x => x.PaidFromAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Expenses)
            .WithOne(x => x.CreditCardInvoice)
            .HasForeignKey(x => x.CreditCardInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => new { x.CreditCardId, x.ReferenceMonth, x.ReferenceYear })
            .IsUnique()
            .HasDatabaseName("IX_CreditCardInvoices_Card_Period");
    }
}
