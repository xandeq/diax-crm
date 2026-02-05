using Diax.Domain.Common;
using Diax.Domain.Auth;
using Diax.Domain.Customers;
using Diax.Domain.Finance;
using Diax.Domain.Household;
using Diax.Domain.Logs;
using Diax.Domain.Snippets;
using Diax.Domain.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.UserGroups;
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

    private readonly ICurrentUserService? _currentUserService;

    public DiaxDbContext(
        DbContextOptions<DiaxDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // ===== DbSets =====
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<IncomeCategory> IncomeCategories => Set<IncomeCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardGroup> CreditCardGroups => Set<CreditCardGroup>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<CreditCardInvoice> CreditCardInvoices => Set<CreditCardInvoice>();
    public DbSet<AccountTransfer> AccountTransfers => Set<AccountTransfer>();
    public DbSet<StatementImport> StatementImports => Set<StatementImport>();
    public DbSet<ImportedTransaction> ImportedTransactions => Set<ImportedTransaction>();
    public DbSet<AppLog> AppLogs => Set<AppLog>();
    public DbSet<UserPrompt> UserPrompts => Set<UserPrompt>();
    public DbSet<Snippet> Snippets => Set<Snippet>();
    public DbSet<ChecklistCategory> ChecklistCategories => Set<ChecklistCategory>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // AI & RBAC
    public DbSet<AiProvider> AiProviders => Set<AiProvider>();
    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
    public DbSet<GroupAiProviderAccess> GroupAiProviderAccesses => Set<GroupAiProviderAccess>();
    public DbSet<GroupAiModelAccess> GroupAiModelAccesses => Set<GroupAiModelAccess>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

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

        // ===== MULTI-TENANCY CONFIG & FILTERS =====
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IUserOwnedEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Configura UserId como obrigatório
                modelBuilder.Entity(entityType.ClrType).Property("UserId").IsRequired();

                // Cria índice para UserId
                modelBuilder.Entity(entityType.ClrType).HasIndex("UserId");
            }
        }

        // Filtra automaticamente entidades que pertencem a um usuário
        modelBuilder.Entity<FinancialAccount>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<Income>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<IncomeCategory>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<ExpenseCategory>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<CreditCard>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<CreditCardGroup>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<CreditCardInvoice>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<AccountTransfer>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<StatementImport>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);
        modelBuilder.Entity<ImportedTransaction>().HasQueryFilter(e => _currentUserService == null || _currentUserService.UserId == null || e.UserId == _currentUserService.UserId);

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
