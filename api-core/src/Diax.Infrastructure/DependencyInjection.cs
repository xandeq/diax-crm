using Diax.Application.Ads;
using Diax.Application.GoogleAnalytics;
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
using Diax.Domain.Helpdesk;
using Diax.Domain.Tasks;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.WhatsApp;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Options;
using Diax.Infrastructure.EmailImages;
using Diax.Infrastructure.WhatsApp;
using Diax.Application.AI;
using Diax.Application.AiChat;
using Diax.Domain.Agents;
using Diax.Domain.AiChat;
using Diax.Infrastructure.AiChat;
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
        services.AddScoped<Diax.Domain.ErrorLogs.IErrorLogRepository, Diax.Infrastructure.Data.Repositories.ErrorLogRepository>();
        services.AddScoped<ISnippetRepository, SnippetRepository>();
        services.AddScoped<ISnippetAttachmentRepository, SnippetAttachmentRepository>();
        services.AddScoped<IUserPromptRepository, UserPromptRepository>();
        services.AddScoped<IChecklistCategoryRepository, ChecklistCategoryRepository>();
        services.AddScoped<IChecklistItemRepository, ChecklistItemRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<Diax.Domain.Briefings.IDailyBriefingRepository, DailyBriefingRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IAiProviderRepository, AiProviderRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IAiProviderCredentialRepository, AiProviderCredentialRepository>();
        services.AddScoped<IAiUsageLogRepository, AiUsageLogRepository>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository>();
        services.AddScoped<IGroupAiAccessRepository, GroupAiAccessRepository>();
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<IEmailCampaignRepository, EmailCampaignRepository>();
        services.AddScoped<IEmailSendLogRepository, EmailSendLogRepository>();
        services.AddScoped<Diax.Domain.Calendar.IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<Diax.Domain.Calendar.IAppointmentLabelRepository, AppointmentLabelRepository>();

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
        services.AddScoped<Diax.Infrastructure.Finance.Parsers.PdfFileParser>();
        services.AddScoped<IFileParser>(sp => sp.GetRequiredService<Diax.Infrastructure.Finance.Parsers.PdfFileParser>());

        // ===== GOOGLE SHEETS =====
        services.AddScoped<Diax.Application.Finance.IGoogleSheetsService, Diax.Infrastructure.Finance.GoogleSheetsService>();

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

        // Register AnthropicChatClient (raw streaming SSE for /ai-chat page)
        services.AddHttpClient<IAnthropicChatClient, AnthropicChatClient>();

        // AI Chat repository
        services.AddScoped<IAiChatRepository, AiChatRepository>();

        // Agent pending action repository
        services.AddScoped<IAgentPendingActionRepository, AgentPendingActionRepository>();

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
        services.Configure<MailjetSettings>(configuration.GetSection("Mailjet"));
        services.Configure<ResendSettings>(configuration.GetSection("Resend"));
        services.Configure<ElasticEmailSettings>(configuration.GetSection("ElasticEmail"));
        services.Configure<MailerSendSettings>(configuration.GetSection("MailerSend"));
        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));

        // Brevo
        services.AddHttpClient<BrevoEmailSender>();
        services.AddKeyedScoped<IEmailSender>("brevo", (sp, _) => sp.GetRequiredService<BrevoEmailSender>());

        // Mailjet
        services.AddHttpClient<MailjetEmailSender>();
        services.AddKeyedScoped<IEmailSender>("mailjet", (sp, _) => sp.GetRequiredService<MailjetEmailSender>());

        // Resend
        services.AddHttpClient<ResendEmailSender>();
        services.AddKeyedScoped<IEmailSender>("resend", (sp, _) => sp.GetRequiredService<ResendEmailSender>());

        // ElasticEmail
        services.AddHttpClient<ElasticEmailSender>();
        services.AddKeyedScoped<IEmailSender>("elasticemail", (sp, _) => sp.GetRequiredService<ElasticEmailSender>());

        // MailerSend
        services.AddHttpClient<MailerSendEmailSender>();
        services.AddKeyedScoped<IEmailSender>("mailersend", (sp, _) => sp.GetRequiredService<MailerSendEmailSender>());

        // SendGrid
        services.AddHttpClient<SendGridEmailSender>();
        services.AddKeyedScoped<IEmailSender>("sendgrid", (sp, _) => sp.GetRequiredService<SendGridEmailSender>());

        // Fallback não-keyed para partes do sistema que usam IEmailSender diretamente (test email, etc.)
        services.AddScoped<IEmailSender, BrevoEmailSender>();

        // ===== SMTP PROVIDERS (Tier 2 — 4 instâncias de GenericSmtpEmailSender) =====
        services.AddOptions<SmtpProviderSettings>("gmail-smtp")
            .BindConfiguration("SmtpProviders:gmail-smtp");
        services.AddOptions<SmtpProviderSettings>("hostgator-smtp")
            .BindConfiguration("SmtpProviders:hostgator-smtp");
        services.AddOptions<SmtpProviderSettings>("smarterasp-smtp")
            .BindConfiguration("SmtpProviders:smarterasp-smtp");
        services.AddOptions<SmtpProviderSettings>("hostinger-smtp")
            .BindConfiguration("SmtpProviders:hostinger-smtp");

        foreach (var smtpKey in new[] { "gmail-smtp", "hostgator-smtp", "smarterasp-smtp", "hostinger-smtp" })
        {
            var captured = smtpKey;
            services.AddKeyedScoped<IEmailSender>(captured, (sp, _) =>
            {
                var monitor = sp.GetRequiredService<IOptionsMonitor<SmtpProviderSettings>>();
                return new GenericSmtpEmailSender(monitor.Get(captured), sp.GetRequiredService<ILogger<GenericSmtpEmailSender>>());
            });
        }

        // ===== UNIFIED EMAIL DISPATCH =====
        services.Configure<EmailChainOptions>(configuration.GetSection(EmailChainOptions.Section));
        services.AddSingleton<IProviderCircuitBreaker, EmailProviderCircuitBreaker>();
        services.AddScoped<IEmailDispatchService, EmailFallbackOrchestrator>();

        // Brevo Contact Stats Service (para analytics por contato)
        services.AddHttpClient<IBrevoContactStatsService, BrevoContactStatsService>();

        services.AddHostedService<EmailQueueProcessorWorker>();

        // Email Image Storage Service (para imagens inline em emails)
        services.AddScoped<IEmailImageStorageService, EmailImageStorageService>();

        // Name normalization (porta da lógica Python de limpeza de nomes)
        services.AddSingleton<Diax.Application.EmailMarketing.Pro.INameNormalizationService,
            Diax.Application.EmailMarketing.Pro.NameNormalizationService>();

        // Provider health service
        services.AddScoped<Diax.Application.EmailMarketing.Pro.IProviderHealthService,
            Diax.Infrastructure.Email.ProviderHealthService>();

        // Smart preselection service
        services.AddScoped<Diax.Application.EmailMarketing.Pro.ISmartPreselectionService,
            Diax.Application.EmailMarketing.Pro.SmartPreselectionService>();

        // Suppression repository + service
        services.AddScoped<IEmailSuppressionRepository,
            Diax.Infrastructure.Data.Repositories.EmailSuppressionRepository>();
        services.AddScoped<Diax.Application.EmailMarketing.Pro.ISuppressionService,
            Diax.Application.EmailMarketing.Pro.SuppressionService>();

        // ===== WHATSAPP (EVOLUTION API) =====
        services.Configure<EvolutionApiSettings>(configuration.GetSection("EvolutionApi"));
        services.AddHttpClient<IWhatsAppSender, EvolutionApiClient>();

        // ===== FACEBOOK ADS (GRAPH API) =====
        services.AddScoped<IFacebookAdAccountRepository, FacebookAdAccountRepository>();
        services.AddHttpClient<FacebookGraphApiClient>();
        services.AddScoped<IFacebookAdsService, FacebookAdsService>();

        // ===== GOOGLE ANALYTICS 4 (DATA API) =====
        services.AddMemoryCache();
        services.AddHttpClient<IGoogleAnalyticsService, Diax.Infrastructure.GoogleAnalytics.GoogleAnalyticsService>();

        // ===== CONFIGURATION PROVIDERS =====
        services.AddScoped<Diax.Shared.Interfaces.IConfigurationProvider, ExternalServices.ConfigurationProvider>();

        // ===== QUOTA MANAGEMENT =====
        services.AddScoped<Diax.Application.AI.QuotaManagement.IAiQuotaService, Diax.Infrastructure.AI.QuotaManagement.AiQuotaService>();
        services.AddHostedService<Diax.Infrastructure.AI.QuotaManagement.QuotaResetWorker>();

        return services;
    }
}
