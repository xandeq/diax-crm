using Diax.Domain.Common;
using Diax.Domain.Auth;
using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Diax.Infrastructure.Data;

/// <summary>
/// DbContext principal da aplicação.
/// Configurado para SQL Server 2022.
/// </summary>
public class DiaxDbContext : DbContext
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => value.Kind == DateTimeKind.Utc
            ? value
            : value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter = new(
        value => value.HasValue
            ? (value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : value.Value.Kind == DateTimeKind.Local
                    ? value.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : value,
        value => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value);

    public DiaxDbContext(DbContextOptions<DiaxDbContext> options) : base(options)
    {
    }

    // ===== DbSets =====
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Diax.Domain.Finance.Income> Incomes => Set<Diax.Domain.Finance.Income>();
    public DbSet<Diax.Domain.Finance.IncomeCategory> IncomeCategories => Set<Diax.Domain.Finance.IncomeCategory>();
    public DbSet<Diax.Domain.Finance.Expense> Expenses => Set<Diax.Domain.Finance.Expense>();
    public DbSet<Diax.Domain.Finance.CreditCard> CreditCards => Set<Diax.Domain.Finance.CreditCard>();
    public DbSet<Diax.Domain.Finance.FinancialAccount> FinancialAccounts => Set<Diax.Domain.Finance.FinancialAccount>();
    public DbSet<Diax.Domain.Finance.CreditCardInvoice> CreditCardInvoices => Set<Diax.Domain.Finance.CreditCardInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== CONVENÇÕES GLOBAIS =====

        // Configura todas as propriedades DateTime para usar datetime2 (maior precisão)
        // e converter para UTC na leitura/escrita.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime2");

                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(UtcDateTimeConverter);
                    }
                    else
                    {
                        property.SetValueConverter(UtcNullableDateTimeConverter);
                    }
                }
            }
        }

        // Configura todas as propriedades string para usar nvarchar (suporte Unicode)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                {
                    property.SetMaxLength(256); // Default para strings sem tamanho definido
                }
            }
        }

        // ===== CONFIGURAÇÕES DE ENTIDADES =====

        // Aplica todas as configurações de entidades do assembly (IEntityTypeConfiguration)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiaxDbContext).Assembly);

        // ===== NAMING PADRÃO (snake_case) =====
        // Aplica nome padrão para tabelas e colunas.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var schema = entityType.GetSchema();
            var snakeTableName = ToSnakeCase(tableName);
            entityType.SetTableName(snakeTableName);

            var tableId = StoreObjectIdentifier.Table(snakeTableName, schema);
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(tableId);
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // GUID como chave primária - usa sequentialuuid para melhor performance
        configurationBuilder.Properties<Guid>()
            .HaveColumnType("uniqueidentifier");

        // Decimais com precisão padrão para valores monetários
        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Atualiza automaticamente os campos de auditoria
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy("system"); // TODO: Obter do contexto de usuário autenticado
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdated("system"); // TODO: Obter do contexto de usuário autenticado
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Atualiza automaticamente os campos de auditoria (versão síncrona)
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy("system");
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdated("system");
                    break;
            }
        }

        return base.SaveChanges();
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Mantém nomes já em minúsculo/snake_case sem mexer demais.
        var span = value.AsSpan();
        var result = new System.Text.StringBuilder(value.Length + 8);

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (char.IsUpper(c))
            {
                var hasPrev = i > 0;
                var prev = hasPrev ? span[i - 1] : '\0';
                var hasNext = i + 1 < span.Length;
                var next = hasNext ? span[i + 1] : '\0';

                if (hasPrev && prev != '_' && (char.IsLower(prev) || (hasNext && char.IsLower(next))))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
