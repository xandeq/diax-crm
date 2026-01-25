using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ImportedTransactionConfiguration : IEntityTypeConfiguration<ImportedTransaction>
{
    public void Configure(EntityTypeBuilder<ImportedTransaction> builder)
    {
        builder.ToTable("imported_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RawDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasOne(x => x.MatchedExpense)
            .WithMany()
            .HasForeignKey(x => x.MatchedExpenseId)
            .OnDelete(DeleteBehavior.NoAction); // Changed from SetNull to avoid cascade path cycles

        builder.HasOne(x => x.CreatedExpense)
            .WithMany()
            .HasForeignKey(x => x.CreatedExpenseId)
            .OnDelete(DeleteBehavior.NoAction); // Changed from SetNull to avoid cascade path cycles
    }
}
