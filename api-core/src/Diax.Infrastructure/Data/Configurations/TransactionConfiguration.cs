using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.RawBankType)
            .IsRequired(false);

        builder.Property(x => x.RawDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.Details)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.PaymentMethod)
            .IsRequired();

        builder.Property(x => x.IsRecurring)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.TransferGroupId)
            .IsRequired(false);

        builder.Property(x => x.AccountTransferId)
            .IsRequired(false);

        builder.Property(x => x.RecurringTransactionId)
            .IsRequired(false);

        builder.Property(x => x.IsSubscription)
            .IsRequired();

        // TransactionCategory relationship
        builder.HasOne(x => x.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // FinancialAccount relationship
        builder.HasOne(x => x.FinancialAccount)
            .WithMany(a => a.Transactions)
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // CreditCard relationship
        builder.HasOne(x => x.CreditCard)
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // CreditCardInvoice relationship
        builder.HasOne(x => x.CreditCardInvoice)
            .WithMany(i => i.Transactions)
            .HasForeignKey(x => x.CreditCardInvoiceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<Diax.Domain.Finance.Planner.RecurringTransaction>()
            .WithMany()
            .HasForeignKey(x => x.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // AccountTransfer relationship
        builder.HasOne(x => x.AccountTransfer)
            .WithMany()
            .HasForeignKey(x => x.AccountTransferId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Indexes para queries frequentes
        builder.HasIndex(x => new { x.UserId, x.Type, x.Date })
            .HasDatabaseName("IX_transactions_user_type_date");

        builder.HasIndex(x => new { x.UserId, x.FinancialAccountId })
            .HasDatabaseName("IX_transactions_user_account");

        builder.HasIndex(x => x.TransferGroupId)
            .HasDatabaseName("IX_transactions_transfer_group")
            .HasFilter("[transfer_group_id] IS NOT NULL");

        builder.HasIndex(x => new { x.UserId, x.Date })
            .HasDatabaseName("IX_transactions_user_date");

        builder.HasIndex(x => new { x.UserId, x.CategoryId })
            .HasDatabaseName("IX_transactions_user_category");
    }
}
