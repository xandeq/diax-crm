using Diax.Application.Customers;
using Diax.Application.Finance;
using Diax.Application.HtmlExtraction;
using Diax.Application.Logs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Diax.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Registra todos os validators do FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Registra os serviços de aplicação
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
        services.AddScoped<AppLogService>();
        services.AddScoped<HtmlExtractionService>();

        return services;
    }
}
