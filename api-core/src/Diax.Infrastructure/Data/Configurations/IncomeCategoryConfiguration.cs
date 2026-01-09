using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class IncomeCategoryConfiguration : IEntityTypeConfiguration<IncomeCategory>
{
    public void Configure(EntityTypeBuilder<IncomeCategory> builder)
    {
        builder.ToTable("income_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
            Create(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Salário"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Serviço"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Marketing"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Venda"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Comissão"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Reembolso"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Empréstimo"),
            Create(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Outros")
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
