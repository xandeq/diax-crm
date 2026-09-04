namespace Diax.Application.Customers;

/// <summary>
/// Configuração do pull agendado de leads do Extrator de Dados (seção "ExtractorPull").
/// Override por variável de ambiente: DIAX_ExtractorPull__Enabled, DIAX_ExtractorPull__DailyHourUtc, etc.
/// </summary>
public class ExtractorPullOptions
{
    public const string SectionName = "ExtractorPull";

    /// <summary>
    /// Liga/desliga o worker de pull diário. Default: false (opt-in explícito por config/env).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Hora UTC em que o pull diário roda. Default: 15 (= 12:00 BRT), DEPOIS da janela
    /// de envio de e-mails da manhã — lead novo nunca entra no envio do mesmo dia sem triagem.
    /// </summary>
    public int DailyHourUtc { get; set; } = 15;

    /// <summary>
    /// Máximo de páginas por pull (100 leads/página).
    /// </summary>
    public int MaxPages { get; set; } = 10;

    /// <summary>
    /// Domínios de e-mail bloqueados no import do Extrator (lixo conhecido observado nos lotes).
    /// Comparação exata pelo domínio (case-insensitive); aceita valor com ou sem '@' inicial.
    /// </summary>
    public List<string> BlockedDomains { get; set; } = new()
    {
        "sun.com",
        "blok.ai",
        "overchat.ai",
        "redcross.org",
        "fox.com",
        "foxtv.com"
    };

    /// <summary>
    /// TLDs estrangeiros óbvios bloqueados no import do Extrator (sufixo do domínio).
    /// NUNCA incluir .com ou .com.br aqui.
    /// </summary>
    public List<string> BlockedTlds { get; set; } = new()
    {
        ".es",
        ".fi",
        ".eu",
        ".ar",
        ".cl",
        ".mx",
        ".pt"
    };
}
