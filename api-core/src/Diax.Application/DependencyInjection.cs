using Diax.Application.Audit;
using Diax.Application.Auth;
using Diax.Application.Customers;
using Diax.Application.Finance;
using Diax.Application.HtmlExtraction;
using Diax.Application.PromptGenerator;
using Diax.Application.Ai.HumanizeText;
using Diax.Application.Household;
using Diax.Application.Logs;
using Diax.Application.Snippets;
using Diax.Application.AI;
using Diax.Application.AI.EmailOptimization;
using Diax.Application.AI.LeadPersona;
using Diax.Application.AI.ImageGeneration;
using Diax.Application.AI.VideoGeneration;
using Diax.Application.AI.Services;
using Diax.Application.ApiKeys;
using Diax.Application.Blog;
using Diax.Application.Blog.Services;
using Diax.Application.Customers.Services;
using Diax.Application.EmailMarketing;
using Diax.Application.Outreach;
using Diax.Application.TaxDocuments;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Diax.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // HttpClient para chamadas externas
        services.AddHttpClient();

        // Registra todos os validators do FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuditLogService, AuditLogService>();

        // Registra os serviços de aplicação
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IUserGroupService, UserGroupService>();
        services.AddScoped<Diax.Application.Calendar.IAppointmentService, Diax.Application.Calendar.AppointmentService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<CustomerImportService>();
        services.AddScoped<ILeadSanitizationService, LeadSanitizationService>();
        services.AddScoped<IncomeService>();
        services.AddScoped<IncomeCategoryService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<ExpenseCategoryService>();
        services.AddScoped<CreditCardService>();
        services.AddScoped<CreditCardGroupService>();
        services.AddScoped<FinancialAccountService>();
        services.AddScoped<CreditCardInvoiceService>();
        services.AddScoped<FinancialSummaryService>();
        services.AddScoped<AccountTransferService>();
        services.AddScoped<StatementImportService>();

        // ===== TRANSACTION (UNIFIED) SERVICES =====
        services.AddScoped<TransactionService>();
        services.AddScoped<TransactionCategoryService>();

        services.AddScoped<AppLogService>();
        services.AddScoped<IAppLogService, AppLogService>();
        services.AddScoped<HtmlExtractionService>();
        services.AddScoped<IPromptGeneratorService, PromptGeneratorService>();
        services.AddScoped<IHumanizeTextService, HumanizeTextService>();
        services.AddScoped<IEmailSubjectOptimizerService, EmailSubjectOptimizerService>();
        services.AddScoped<ILeadPersonaGeneratorService, LeadPersonaGeneratorService>();
        services.AddScoped<EmailMarketingService>();
        services.AddScoped<OutreachService>();
        services.AddSingleton<IEmailTemplateEngine, EmailTemplateEngine>();
        services.AddScoped<SnippetService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IChecklistCategoryService, ChecklistCategoryService>();
        services.AddScoped<IChecklistItemService, ChecklistItemService>();
        services.AddScoped<IAiProviderAdminService, AiProviderAdminService>();
        services.AddScoped<IAiCatalogService, AiCatalogService>();
        services.AddScoped<IGroupAiAccessService, GroupAiAccessService>();
        services.AddScoped<Diax.Application.AI.Interfaces.IAiInsightsService, AiInsightsService>();

        // AI Model Validator com cache (fonte única de verdade = banco)
        services.AddScoped<IAiModelValidator, AiModelValidator>();
        services.AddScoped<IAiProviderManagementService, AiProviderManagementService>();

        // API Key Encryption Service
        services.AddSingleton<IApiKeyEncryptionService, ApiKeyEncryptionService>();

        // AI Usage Tracking Service
        services.AddScoped<IAiUsageTrackingService, AiUsageTrackingService>();

        // AI Image Generation Service
        services.AddScoped<IImageGenerationService, ImageGenerationService>();

        // AI Video Generation Service
        services.AddScoped<IVideoGenerationService, VideoGenerationService>();

        // Memory Cache para AiModelValidator
        services.AddMemoryCache();

        // ===== EXTRATOR INTEGRATION =====
        services.AddScoped<IExtractorService, ExtractorService>();

        // ===== BLOG & API KEYS SERVICES =====
        services.AddScoped<ApiKeyService>();
        services.AddScoped<BlogPostService>();
        services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddScoped<ISlugGeneratorService, SlugGeneratorService>();

        // ===== TAX DOCUMENTS =====
        services.AddScoped<ITaxDocumentService, TaxDocumentService>();

        // ===== FINANCIAL PLANNER SERVICES =====
        services.AddScoped<Diax.Application.Finance.Planner.FinancialGoalService>();
        services.AddScoped<Diax.Application.Finance.Planner.RecurringTransactionService>();
        services.AddScoped<Diax.Application.Finance.Planner.CashFlowProjectionService>();
        services.AddScoped<Diax.Application.Finance.Planner.MonthlySimulationService>();

        return services;
    }
}
