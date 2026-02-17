using Diax.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    public void Configure(EntityTypeBuilder<TransactionCategory> builder)
    {
        builder.ToTable("transaction_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ApplicableTo)
            .IsRequired();

        // Seed data - Income categories (mesmos IDs do IncomeCategory original)
        builder.HasData(
            // Income categories
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Salário", CategoryApplicableTo.Income),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Serviço", CategoryApplicableTo.Income),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Marketing", CategoryApplicableTo.Income),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Venda", CategoryApplicableTo.Income),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Comissão", CategoryApplicableTo.Income),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Reembolso", CategoryApplicableTo.Both),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Empréstimo", CategoryApplicableTo.Both),
            CreateSeed(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Outros", CategoryApplicableTo.Both),

            // Expense categories (mesmos IDs do ExpenseCategory original)
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Alimentação", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Transporte", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Moradia", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000004"), "Saúde", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000005"), "Educação", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000006"), "Lazer", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000007"), "Vestuário", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Serviços", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Impostos", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000010"), "Investimentos", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000011"), "Marketing", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000012"), "Equipamentos", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000013"), "Fornecedores", CategoryApplicableTo.Expense),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000014"), "Não Categorizado", CategoryApplicableTo.Both),
            CreateSeed(Guid.Parse("20000000-0000-0000-0000-000000000015"), "Outros", CategoryApplicableTo.Both)
        );
    }

    private static object CreateSeed(Guid id, string name, CategoryApplicableTo applicableTo)
    {
        return new
        {
            Id = id,
            Name = name,
            ApplicableTo = applicableTo,
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
