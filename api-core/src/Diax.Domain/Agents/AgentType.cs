namespace Diax.Domain.Agents;

/// <summary>
/// Tipos de agente de IA do DIAX CRM.
/// Cada tipo compartilha o mesmo motor de chat, variando apenas
/// system prompt, ferramentas e escopo de dados.
/// </summary>
public enum AgentType
{
    /// <summary>Assistente de produtividade pessoal (agenda, financeiro).</summary>
    Personal = 0,

    /// <summary>Atendimento/suporte ao cliente (histórico, tickets).</summary>
    Support = 1,

    /// <summary>Agente comercial/SDR (qualificação de leads, outreach).</summary>
    Commercial = 2,
}
