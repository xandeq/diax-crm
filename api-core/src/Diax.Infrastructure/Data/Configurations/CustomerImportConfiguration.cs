using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração do Entity Framework para a entidade CustomerImport.
/// </summary>
public class CustomerImportConfiguration : IEntityTypeConfiguration<CustomerImport>
{
    public void Configure(EntityTypeBuilder<CustomerImport> builder)
    {
        builder.ToTable("customer_imports");

        // Primary Key
        builder.HasKey(x => x.Id);

        // Propriedades
        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.TotalRecords)
            .IsRequired();

        builder.Property(x => x.SuccessCount)
            .IsRequired();

        builder.Property(x => x.FailedCount)
            .IsRequired();

        builder.Property(x => x.ErrorDetails)
            .HasColumnType("nvarchar(max)");

        // Propriedades de auditoria
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        // Índices
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_CustomerImports_CreatedAt");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_CustomerImports_Status");
    }
}
