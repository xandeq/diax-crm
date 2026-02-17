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

        // Novos relacionamentos unificados (Transaction)
        builder.HasOne(x => x.CreatedTransaction)
            .WithMany()
            .HasForeignKey(x => x.CreatedTransactionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.MatchedTransaction)
            .WithMany()
            .HasForeignKey(x => x.MatchedTransactionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Legacy relationships (mantidas para período de migração)
        builder.HasOne(x => x.MatchedExpense)
            .WithMany()
            .HasForeignKey(x => x.MatchedExpenseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CreatedExpense)
            .WithMany()
            .HasForeignKey(x => x.CreatedExpenseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CreatedIncome)
            .WithMany()
            .HasForeignKey(x => x.CreatedIncomeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
