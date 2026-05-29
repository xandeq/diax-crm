using Diax.Domain.Agents;

namespace Diax.Application.Agents.Commercial.Dtos;

/// <summary>
/// Requisição de chat do Agente Comercial.
/// Stateless — o cliente envia o histórico; nenhuma conversa é persistida nesta versão.
/// </summary>
/// <param name="Message">Mensagem atual do usuário.</param>
/// <param name="History">Histórico anterior (roles "user"/"assistant").</param>
/// <param name="LeadIds">IDs específicos de leads/clientes para anexar ao contexto. Tem precedência sobre Segment.</param>
/// <param name="Segment">Filtro por segmento quando LeadIds não é informado: "Hot", "Warm" ou "Cold".</param>
/// <param name="OnlyLeads">Quando true (default), limita o contexto a leads (Status &lt; Customer).</param>
/// <param name="MaxLeads">Máximo de leads carregados no contexto (1..50).</param>
/// <param name="Model">Modelo Anthropic. Default: claude-sonnet-4-5.</param>
public record CommercialAgentRequest(
    string Message,
    List<AgentChatMessageDto>? History = null,
    List<Guid>? LeadIds = null,
    string? Segment = null,
    bool OnlyLeads = true,
    int MaxLeads = 25,
    string? Model = null);

/// <summary>Mensagem de histórico. Role deve ser "user" ou "assistant".</summary>
public record AgentChatMessageDto(string Role, string Content);

/// <summary>Resposta do Agente Comercial.</summary>
public record CommercialAgentResponse(
    AgentType Agent,
    string Reply,
    int LeadsInContext,
    string Model,
    AgentUsageDto Usage);

/// <summary>Uso de tokens e custo estimado da chamada.</summary>
public record AgentUsageDto(int InputTokens, int OutputTokens, decimal CostUsd);
