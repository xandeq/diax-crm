using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Diax.Application.AI.Interfaces;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.Services;

public class AiInsightsService : IAiInsightsService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly IOpenAiClient _openAiClient;
    private readonly ILogger<AiInsightsService> _logger;

    public AiInsightsService(
        ICustomerRepository customerRepository,
        IEmailCampaignRepository emailCampaignRepository,
        IOpenAiClient openAiClient,
        ILogger<AiInsightsService> logger)
    {
        _customerRepository = customerRepository;
        _emailCampaignRepository = emailCampaignRepository;
        _openAiClient = openAiClient;
        _logger = logger;
    }

    public async Task<string> GetDailyInsightsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating daily AI insights for user {UserId}", userId);

        try
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-7);

            // Gather metrics concurrently
            var customersTask = _customerRepository.GetPagedAsync(1, 1000, createdAfter: today, cancellationToken: cancellationToken);
            var campaignsTask = _emailCampaignRepository.GetPagedByUserAsync(userId, 1, 1000, cancellationToken: cancellationToken);

            await Task.WhenAll(customersTask, campaignsTask);

            var customers = customersTask.Result;
            var campaigns = campaignsTask.Result;

            var totalLeadsToday = customers.Items.Count(c => c.Status < CustomerStatus.Customer);
            var totalCustomersToday = customers.Items.Count(c => c.Status >= CustomerStatus.Customer);
            var recentCampaigns = campaigns.Items.Where(c => c.ScheduledAt >= sevenDaysAgo || c.CreatedAt >= sevenDaysAgo).ToList();

            var totalEmailsSent = recentCampaigns.Sum(c => c.SentCount);
            var totalEmailsOpened = recentCampaigns.Sum(c => c.OpenCount);

            var systemData = $@"
CRM Daily Stats:
- New Leads Today: {totalLeadsToday}
- New Conversions/Customers Today: {totalCustomersToday}
- Emails Sent (Last 7 days): {totalEmailsSent}
- Emails Opened (Last 7 days): {totalEmailsOpened}
";

            var prompt = @"
Você é um consultor estratégico AI de marketing e vendas analisando o CRM (CEO dashboard).
Sua tarefa é ler os dados de hoje apontados abaixo e gerar um pequeno resumo inspirador.
Destaque o que está funcionando e o que o empreendedor deve focar agora. Por exemplo, se há muitos leads e poucos emails, sugira automatizar envios.
Seja conciso, profissional, em português (Brasil), e limite-se a 1 ou 2 parágrafos.
Use alguns emojis estrategicamente.

Dados Atuais:
" + systemData;

            var request = new OpenAiChatCompletionRequest
            {
                Model = "gpt-4o-mini", // Cost efficient model
                Temperature = 0.7,
                Messages = new List<OpenAiMessage>
                {
                    new OpenAiMessage { Role = "system", Content = "You are a helpful business analytics AI." },
                    new OpenAiMessage { Role = "user", Content = prompt }
                }
            };

            var response = await _openAiClient.CreateChatCompletionAsync(request, cancellationToken);

            if (response != null && response.Choices.Count > 0)
            {
                return response.Choices[0].Message.Content;
            }

            return "Nenhum insight disponível no momento.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI insights for user {UserId}", userId);
            return "Não foi possível analisar os dados agora. Consulte novamente mais tarde.";
        }
    }
}
