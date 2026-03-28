using Diax.Application.Ads;
using Diax.Domain.Ads.Repositories;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Infrastructure.Ads;
using Diax.Infrastructure.Data.Interceptors;
using Diax.Infrastructure.Data.Repositories;
using Diax.Infrastructure.Identity;
using Diax.Domain.Customers;
using Diax.Domain.Finance;
using Diax.Domain.Household;
using Diax.Domain.Logs;
using Diax.Domain.Snippets;
using Diax.Domain.PromptGenerator;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Finance;
using Diax.Infrastructure.Ai;
using Diax.Infrastructure.AI.QuotaManagement;
using Diax.Shared.Ai;
using Diax.Domain.AI;
using Diax.Domain.UserGroups;
using Diax.Domain.ApiKeys;
using Diax.Domain.Blog;
using Diax.Domain.EmailMarketing;
using Diax.Domain.ImageGeneration;
using Diax.Domain.Outreach;
using Diax.Application.EmailMarketing;
using Diax.Application.WhatsApp;
using Diax.Infrastructure.Email;
using Diax.Infrastructure.EmailImages;
using Diax.Infrastructure.WhatsApp;
using Diax.Application.AI;
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
        // ===== CACHE =====
        // AddMemoryCache() — for IMemoryCache (used by ConfigurationProvider)
        // AddDistributedMemoryCache() — for IDistributedCache (if needed later)
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        // ===== CONNECTION STRING =====
        // Prioridade: 1) Variável de ambiente, 2) User Secrets, 3) appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Use fallback for graceful degradation - will fail during DB access with better error message
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Database=DiaxCRM;Integrated Security=false;";
            Console.WriteLine("WARNING: Connection string 'DefaultConnection' not found. Using fallback (DB operations will fail).");
        }

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

        // ===== AUDIT INTERCEPTOR =====
        // Deve ser registrado antes do DbContext para ser resolvido dentro da factory
        services.AddScoped<AuditSaveChangesInterceptor>();

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

            // Registra o interceptor de auditoria na mesma scope do DbContext
            var auditInterceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
            options.AddInterceptors(auditInterceptor);

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

        // ===== IDENTITY & CURRENT USER =====
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ===== REPOSITÓRIOS =====
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<Diax.Application.Auth.IPermissionService, Diax.Infrastructure.Auth.PermissionService>();
        services.AddScoped<Diax.Application.Customers.IApifyIntegrationService, Diax.Application.Customers.ApifyIntegrationService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerImportRepository, CustomerImportRepository>();
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IIncomeCategoryRepository, IncomeCategoryRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICreditCardGroupRepository, CreditCardGroupRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<ICreditCardInvoiceRepository, CreditCardInvoiceRepository>();
        services.AddScoped<IAccountTransferRepository, AccountTransferRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionCategoryRepository, TransactionCategoryRepository>();
        services.AddScoped<IStatementImportRepository, StatementImportRepository>();
        services.AddScoped<IImportedTransactionRepository, ImportedTransactionRepository>();
        services.AddScoped<IAppLogRepository, AppLogRepository>();
        services.AddScoped<ISnippetRepository, SnippetRepository>();
        services.AddScoped<IUserPromptRepository, UserPromptRepository>();
        services.AddScoped<IChecklistCategoryRepository, ChecklistCategoryRepository>();
        services.AddScoped<IChecklistItemRepository, ChecklistItemRepository>();
        services.AddScoped<IAiProviderRepository, AiProviderRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IAiProviderCredentialRepository, AiProviderCredentialRepository>();
        services.AddScoped<IAiUsageLogRepository, AiUsageLogRepository>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository>();
        services.AddScoped<IGroupAiAccessRepository, GroupAiAccessRepository>();
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<IEmailCampaignRepository, EmailCampaignRepository>();
        services.AddScoped<Diax.Domain.Calendar.IAppointmentRepository, AppointmentRepository>();

        // ===== TAX DOCUMENTS =====
        services.AddScoped<Diax.Domain.TaxDocuments.ITaxDocumentRepository, TaxDocumentRepository>();

        // ===== OUTREACH =====
        services.AddScoped<IOutreachConfigRepository, OutreachConfigRepository>();

        // ===== IMAGE GENERATION =====
        services.AddScoped<IImageTemplateRepository, ImageTemplateRepository>();
        services.AddScoped<IImageGenerationProjectRepository, ImageGenerationProjectRepository>();
        services.AddScoped<IGeneratedImageRepository, GeneratedImageRepository>();

        // ===== AUDIT LOG =====
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // ===== BLOG & API KEYS =====
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();

        // ===== FINANCIAL PLANNER =====
        services.AddScoped<Diax.Domain.Finance.Planner.Repositories.IFinancialGoalRepository, Diax.Infrastructure.Finance.Planner.FinancialGoalRepository>();
        services.AddScoped<Diax.Domain.Finance.Planner.Repositories.IRecurringTransactionRepository, Diax.Infrastructure.Finance.Planner.RecurringTransactionRepository>();
        services.AddScoped<Diax.Domain.Finance.Planner.Repositories.IMonthlySimulationRepository, Diax.Infrastructure.Finance.Planner.MonthlySimulationRepository>();
        services.AddScoped<Diax.Domain.Finance.Planner.Repositories.ICreditCardStrategyRepository, Diax.Infrastructure.Finance.Planner.CreditCardStrategyRepository>();

        // ===== PARSERS =====
        services.AddScoped<IFileParser, Diax.Infrastructure.Finance.Parsers.CsvFileParser>();
        services.AddScoped<IFileParser, Diax.Infrastructure.Finance.Parsers.PdfFileParser>();

        // ===== AI CLIENTS =====
        services.AddScoped<IAiTextTransformClient, ChatGptClient>();
        services.AddScoped<IAiTextTransformClient, PerplexityClient>();
        services.AddScoped<IAiTextTransformClient, DeepSeekClient>();
        services.AddScoped<IAiTextTransformClient, OpenRouterTextTransformClient>();

        // Register OpenRouterClient as typed HttpClient
        services.AddHttpClient<IOpenRouterClient, OpenRouterClient>();

        // Register OpenAiClient as typed HttpClient
        services.AddHttpClient<IOpenAiClient, OpenAiClient>();

        // Register GeminiClient as typed HttpClient
        services.AddHttpClient<IGeminiClient, GeminiClient>();

        // Register DeepSeekModelClient as typed HttpClient
        services.AddHttpClient<IDeepSeekModelClient, DeepSeekModelClient>();

        // Register AnthropicClient as typed HttpClient
        services.AddHttpClient<IAnthropicClient, AnthropicClient>();

        // Register AnthropicTextTransformClient
        services.AddHttpClient<AnthropicTextTransformClient>();
        services.AddScoped<IAiTextTransformClient, AnthropicTextTransformClient>();

        // Register GrokClient as typed HttpClient
        services.AddHttpClient<IGrokClient, GrokClient>();

        // Register GrokTextTransformClient
        services.AddHttpClient<GrokTextTransformClient>();
        services.AddScoped<IAiTextTransformClient, GrokTextTransformClient>();

        // Register GroqClient (text generation via OpenAI-compatible API)
        services.AddHttpClient<GroqClient>();
        services.AddScoped<IAiTextTransformClient, GroqClient>();

        // Register CerebrasClient (text generation via OpenAI-compatible API)
        services.AddHttpClient<CerebrasClient>();
        services.AddScoped<IAiTextTransformClient, CerebrasClient>();

        // Register HuggingFaceTextClient (text generation via Serverless Inference API)
        services.AddHttpClient<HuggingFaceTextClient>();
        services.AddScoped<IAiTextTransformClient, HuggingFaceTextClient>();

        // ===== AI IMAGE GENERATION CLIENTS =====
        services.AddHttpClient<OpenAiImageClient>();
        services.AddScoped<IAiImageGenerationClient, OpenAiImageClient>();

        services.AddHttpClient<OpenRouterImageClient>();
        services.AddScoped<IAiImageGenerationClient, OpenRouterImageClient>();

        services.AddHttpClient<GeminiImageClient>();
        services.AddScoped<IAiImageGenerationClient, GeminiImageClient>();

        services.AddHttpClient<FalAiImageClient>();
        services.AddScoped<IAiImageGenerationClient, FalAiImageClient>();

        services.AddHttpClient<GrokImageClient>();
        services.AddScoped<IAiImageGenerationClient, GrokImageClient>();

        services.AddHttpClient<HuggingFaceImageClient>();
        services.AddScoped<IAiImageGenerationClient, HuggingFaceImageClient>();

        // ===== AI VIDEO GENERATION CLIENTS =====
        services.AddHttpClient<FalAiVideoClient>();
        services.AddScoped<IAiVideoGenerationClient, FalAiVideoClient>();

        services.AddHttpClient<HuggingFaceVideoClient>();
        services.AddScoped<IAiVideoGenerationClient, HuggingFaceVideoClient>();

        services.AddHttpClient<RunwayVideoClient>();
        services.AddScoped<IAiVideoGenerationClient, RunwayVideoClient>();

        services.AddHttpClient<ReplicateVideoClient>();
        services.AddScoped<IAiVideoGenerationClient, ReplicateVideoClient>();

        services.AddHttpClient<ShotstackVideoClient>();
        services.AddScoped<IAiVideoGenerationClient, ShotstackVideoClient>();

        // ===== EMAIL MARKETING =====
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<BrevoSettings>(configuration.GetSection("Brevo"));

        // Brevo como sender principal se configurado, SMTP como fallback
        var brevoApiKey = configuration.GetSection("Brevo:ApiKey").Value;
        if (!string.IsNullOrWhiteSpace(brevoApiKey))
        {
            services.AddHttpClient<BrevoEmailSender>();
            services.AddScoped<IEmailSender, BrevoEmailSender>();

            // Brevo Contact Stats Service (para analytics por contato)
            services.AddHttpClient<IBrevoContactStatsService, BrevoContactStatsService>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        services.AddHostedService<EmailQueueProcessorWorker>();

        // Email Image Storage Service (para imagens inline em emails)
        services.AddScoped<IEmailImageStorageService, EmailImageStorageService>();

        // ===== WHATSAPP (EVOLUTION API) =====
        services.Configure<EvolutionApiSettings>(configuration.GetSection("EvolutionApi"));
        services.AddHttpClient<IWhatsAppSender, EvolutionApiClient>();

        // ===== FACEBOOK ADS (GRAPH API) =====
        services.AddScoped<IFacebookAdAccountRepository, FacebookAdAccountRepository>();
        services.AddHttpClient<FacebookGraphApiClient>();
        services.AddScoped<IFacebookAdsService, FacebookAdsService>();

        // ===== CONFIGURATION PROVIDERS =====
        services.AddScoped<Diax.Shared.Interfaces.IConfigurationProvider, ExternalServices.ConfigurationProvider>();

        // ===== QUOTA MANAGEMENT =====
        services.AddScoped<Diax.Application.AI.QuotaManagement.IAiQuotaService, Diax.Infrastructure.AI.QuotaManagement.AiQuotaService>();
        services.AddHostedService<Diax.Infrastructure.AI.QuotaManagement.QuotaResetWorker>();

        return services;
    }
}
