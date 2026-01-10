using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasData(
            Create(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Alimentação"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Transporte"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Moradia"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000004"), "Saúde"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000005"), "Educação"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000006"), "Lazer"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000007"), "Vestuário"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Serviços"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Impostos"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000010"), "Investimentos"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000011"), "Marketing"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000012"), "Equipamentos"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000013"), "Fornecedores"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000014"), "Não Categorizado"),
            Create(Guid.Parse("20000000-0000-0000-0000-000000000015"), "Outros")
        );
    }

    private static object Create(Guid id, string name)
    {
        return new
        {
            Id = id,
            Name = name,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
