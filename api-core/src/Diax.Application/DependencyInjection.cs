using Diax.Application.Auth;
using Diax.Application.Customers;
using Diax.Application.Finance;
using Diax.Application.HtmlExtraction;
using Diax.Application.PromptGenerator;
using Diax.Application.Ai.HumanizeText;
using Diax.Application.Ai.Services;
using Diax.Application.Household;
using Diax.Application.Logs;
using Diax.Application.Snippets;
using Diax.Application.AI;
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

        // Registra os serviços de aplicação
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IUserGroupService, UserGroupService>();
        services.AddScoped<CustomerService>();
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
        services.AddScoped<AppLogService>();
        services.AddScoped<IAppLogService, AppLogService>();
        services.AddScoped<HtmlExtractionService>();
        services.AddScoped<IPromptGeneratorService, PromptGeneratorService>();
        services.AddScoped<IHumanizeTextService, HumanizeTextService>();
        services.AddScoped<SnippetService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IChecklistCategoryService, ChecklistCategoryService>();
        services.AddScoped<IChecklistItemService, ChecklistItemService>();
        services.AddScoped<IAiProviderAdminService, AiProviderAdminService>();
        services.AddScoped<IAiCatalogService, AiCatalogService>();
        services.AddScoped<IGroupAiAccessService, GroupAiAccessService>();

        // AI Model Validator com cache (fonte única de verdade = banco)
        services.AddScoped<IAiModelValidator, AiModelValidator>();
        services.AddScoped<IAiProviderManagementService, AiProviderManagementService>();

        // AI Usage Logging
        services.AddScoped<ITokenEstimator, TokenEstimator>();
        services.AddScoped<IAiUsageLogService, AiUsageLogService>();

        // Memory Cache para AiModelValidator
        services.AddMemoryCache();

        return services;
    }
}