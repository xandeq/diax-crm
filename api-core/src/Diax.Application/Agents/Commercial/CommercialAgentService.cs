using Diax.Application.Agents.Commercial.Dtos;
using Diax.Application.AiChat;
using Diax.Domain.Agents;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Agents.Commercial;

/// <summary>
/// Agente Comercial — reaproveita o gateway LLM (IAnthropicChatClient) e o repositório
/// de Customers para responder com base no pipeline real de leads. Stateless nesta versão.
/// </summary>
public class CommercialAgentService : ICommercialAgentService
{
    public const string DefaultModel = "claude-sonnet-4-5";
    public const int DefaultMaxTokens = 2048;
    public const int MaxLeadsCap = 50;
    public const int DefaultMaxLeads = 25;

    private readonly ICustomerRepository _customers;
    private readonly IAnthropicChatClient _anthropic;
    private readonly ILogger<CommercialAgentService> _logger;

    public CommercialAgentService(
        ICustomerRepository customers,
        IAnthropicChatClient anthropic,
        ILogger<CommercialAgentService> logger)
    {
        _customers = customers;
        _anthropic = anthropic;
        _logger = logger;
    }

    public async Task<Result<CommercialAgentResponse>> ChatAsync(
        Guid userId,
        CommercialAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return Result.Failure<CommercialAgentResponse>(new Error("Validation", "Mensagem é obrigatória."));

        var model = string.IsNullOrWhiteSpace(request.Model) ? DefaultModel : request.Model!.Trim();

        // ----- 1. Contexto de leads -----
        var leads = await LoadLeadsAsync(request, cancellationToken);

        // ----- 2. System prompt + contexto -----
        var system = CommercialAgentPrompts.BuildSystemPrompt(leads);

        // ----- 3. Histórico + mensagem atual -----
        var messages = new List<AnthropicMessage>();
        if (request.History is not null)
        {
            foreach (var m in request.History)
            {
                if (string.IsNullOrWhiteSpace(m.Content)) continue;
                if (m.Role != "user" && m.Role != "assistant") continue;
                messages.Add(new AnthropicMessage(m.Role, m.Content));
            }
        }
        messages.Add(new AnthropicMessage("user", request.Message));

        var anthropicRequest = new AnthropicMessageRequest(
            Model: model,
            Messages: messages,
            MaxTokens: DefaultMaxTokens,
            System: system,
            EnablePromptCache: true);

        // ----- 4. Chama o LLM (mesmo gateway do AiChat) -----
        AnthropicCompletionResult completion;
        try
        {
            completion = await _anthropic.CompleteAsync(anthropicRequest, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CommercialAgent] Falha ao consultar o LLM (user {UserId})", userId);
            return Result.Failure<CommercialAgentResponse>(
                new Error("Agent.LlmError", "Falha ao consultar o modelo de IA. Tente novamente."));
        }

        var cost = AiChatService.CalculateCost(model, completion.Usage);

        var response = new CommercialAgentResponse(
            Agent: AgentType.Commercial,
            Reply: completion.Text,
            LeadsInContext: leads.Count,
            Model: model,
            Usage: new AgentUsageDto(completion.Usage.InputTokens, completion.Usage.OutputTokens, cost));

        return Result<CommercialAgentResponse>.Success(response);
    }

    /// <summary>
    /// Carrega os leads para o contexto: por IDs explícitos (precedência) ou por segmento.
    /// Ordena por LeadScore desc em memória (determinístico) e limita ao cap.
    /// </summary>
    private async Task<IReadOnlyList<Customer>> LoadLeadsAsync(
        CommercialAgentRequest request,
        CancellationToken ct)
    {
        var max = Math.Clamp(request.MaxLeads <= 0 ? DefaultMaxLeads : request.MaxLeads, 1, MaxLeadsCap);

        IEnumerable<Customer> source;

        if (request.LeadIds is { Count: > 0 })
        {
            source = await _customers.GetByIdsAsync(request.LeadIds.Take(max), ct);
        }
        else
        {
            var segment = ParseSegment(request.Segment);
            var (items, _) = await _customers.GetPagedAsync(
                page: 1,
                pageSize: max,
                segment: segment,
                onlyLeads: request.OnlyLeads ? true : (bool?)null,
                cancellationToken: ct);
            source = items;
        }

        return source
            .OrderByDescending(c => c.LeadScore ?? -1)
            .ThenByDescending(c => c.Segment ?? LeadSegment.Cold)
            .Take(max)
            .ToList();
    }

    private static LeadSegment? ParseSegment(string? segment)
        => Enum.TryParse<LeadSegment>(segment, ignoreCase: true, out var s) ? s : null;
}
