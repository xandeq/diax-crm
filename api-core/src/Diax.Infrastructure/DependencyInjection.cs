using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Finance;
using Diax.Domain.Logs;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Diax.Infrastructure.Finance;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure;

public static class DependencyInjection
{
    private static bool ShouldForceTcp(string? dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return false;
        }

        // If a protocol is already specified, keep it.
        if (dataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("np:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("lpc:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // LocalDB and local instances should NOT be forced to TCP.
        if (dataSource.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("(local)", StringComparison.OrdinalIgnoreCase)
            || dataSource.Equals(".", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith(".\\", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("::1", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Instance names (SERVER\\INSTANCE) rely on SQL Browser; forcing tcp: can break resolution.
        if (dataSource.Contains('\\'))
        {
            return false;
        }

        return true;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ===== CONNECTION STRING =====
        // Prioridade: 1) Variável de ambiente, 2) User Secrets, 3) appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não encontrada. " +
                "Configure via User Secrets (local) ou variável de ambiente (produção).");

        // Normaliza a connection string para ambiente remoto:
        // - Força TCP (evita Named Pipes em cenários remotos)
        // - Garante Encrypt/TrustServerCertificate
        // - Ajusta timeout
        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = Math.Max(30, new SqlConnectionStringBuilder(connectionString).ConnectTimeout)
        };

        // Força TCP apenas para datasources remotos simples.
        // (Não aplicar em LocalDB/localhost/instâncias nomeadas.)
        if (ShouldForceTcp(csb.DataSource))
        {
            csb.DataSource = $"tcp:{csb.DataSource}";
        }

        // Evita reutilizar a string original, usa a normalizada.
        connectionString = csb.ConnectionString;

        // ===== DBCONTEXT - SQL SERVER 2022 =====
        services.AddDbContext<DiaxDbContext>((serviceProvider, options) =>
        {
            #if DEBUG
            try
            {
                var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbConnection");
                var safe = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(safe.Password))
                {
                    safe.Password = "***";
                }

                logger?.LogInformation("EF Core SQL Server: DataSource={DataSource} Database={Database} Encrypt={Encrypt} TrustServerCertificate={TrustServerCertificate}",
                    safe.DataSource,
                    safe.InitialCatalog,
                    safe.Encrypt,
                    safe.TrustServerCertificate);
            }
            catch
            {
                // Não falhar startup/migrations por logging.
            }
            #endif

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Assembly onde estão as Migrations
                sqlOptions.MigrationsAssembly(typeof(DiaxDbContext).Assembly.FullName);

                // Resiliência de conexão (retry automático)
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                // Timeout de comando (60 segundos)
                sqlOptions.CommandTimeout(60);

                // Compatibilidade com SQL Server 2022
                sqlOptions.UseCompatibilityLevel(160); // SQL Server 2022
            });

            // Em desenvolvimento, habilita logs detalhados e dados sensíveis
            #if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            #endif
        });

        // ===== UNIT OF WORK =====
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ===== REPOSITÓRIOS =====
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IIncomeCategoryRepository, IncomeCategoryRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICreditCardGroupRepository, CreditCardGroupRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<ICreditCardInvoiceRepository, CreditCardInvoiceRepository>();
        services.AddScoped<IAccountTransferRepository, AccountTransferRepository>();
        services.AddScoped<IAppLogRepository, AppLogRepository>();

        return services;
    }
}
