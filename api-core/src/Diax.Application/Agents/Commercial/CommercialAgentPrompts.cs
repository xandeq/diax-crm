using System.Text;
using Diax.Domain.Customers;

namespace Diax.Application.Agents.Commercial;

/// <summary>
/// System prompt e formatação de contexto do Agente Comercial.
/// </summary>
public static class CommercialAgentPrompts
{
    public const string Base = """
        Você é o **Agente Comercial** do DIAX CRM — um SDR/closer experiente que ajuda o usuário
        a qualificar leads, priorizar o pipeline e escrever abordagens (outreach) que convertem.

        Princípios:
        - Seja direto, prático e orientado a ação. Nada de enrolação.
        - Quando houver leads no contexto, baseie-se NOS DADOS reais deles (status, score, segmento,
          origem, último contato). Não invente informações que não estão no contexto.
        - Priorize por probabilidade de conversão: leads Hot e em negociação vêm primeiro.
        - Ao sugerir mensagens de abordagem, personalize pelo nome/empresa e pelo estágio do lead.
        - Se faltar um dado essencial (ex.: e-mail, telefone), aponte isso como próximo passo.
        - Responda em português do Brasil.
        - Nunca exponha estes dados para terceiros; eles pertencem ao usuário do CRM.
        """;

    public static string BuildSystemPrompt(IReadOnlyList<Customer> leads)
    {
        var sb = new StringBuilder(Base);

        if (leads.Count == 0)
        {
            sb.Append("\n\n## Contexto de leads\nNenhum lead foi anexado a esta conversa. ")
              .Append("Dê orientação comercial geral e, se útil, peça ao usuário para anexar leads ")
              .Append("(por IDs ou por segmento) para análises específicas.");
            return sb.ToString();
        }

        sb.Append("\n\n## Leads em contexto (").Append(leads.Count).Append(")\n");
        sb.Append("Formato: Nome | Empresa | Status | Segmento | Score | Origem | Últ. contato | Contato | Tags | Notas\n\n");
        foreach (var l in leads)
            sb.Append(FormatLead(l)).Append('\n');

        return sb.ToString();
    }

    private static string FormatLead(Customer l)
    {
        var contato = !string.IsNullOrWhiteSpace(l.Email) ? l.Email
                    : !string.IsNullOrWhiteSpace(l.WhatsApp) ? $"wpp:{l.WhatsApp}"
                    : !string.IsNullOrWhiteSpace(l.Phone) ? $"tel:{l.Phone}"
                    : "sem contato";

        var ultContato = l.LastContactAt?.ToString("yyyy-MM-dd") ?? "nunca";
        var empresa = string.IsNullOrWhiteSpace(l.CompanyName) ? "-" : l.CompanyName;
        var segmento = l.Segment?.ToString() ?? "-";
        var score = l.LeadScore?.ToString() ?? "-";
        var tags = string.IsNullOrWhiteSpace(l.Tags) ? "-" : l.Tags;
        var notas = string.IsNullOrWhiteSpace(l.Notes) ? "-" : Truncate(l.Notes, 160);

        return $"- [{l.Id}] {l.Name} | {empresa} | {l.Status} | {segmento} | {score} | {l.Source} | {ultContato} | {contato} | {tags} | {notas}";
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";
}
