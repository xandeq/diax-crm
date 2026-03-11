using Diax.Domain.TaxDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class TaxDocumentConfiguration : IEntityTypeConfiguration<TaxDocument>
{
    public void Configure(EntityTypeBuilder<TaxDocument> builder)
    {
        builder.ToTable("tax_documents");

        builder.Property(x => x.InstitutionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.StoredFileName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.UserId, x.FiscalYear });
    }
}
