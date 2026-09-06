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

    /// <summary>
    /// UFs aceitas no import do Extrator (negócio local, Grande Vitória-ES). Comparação
    /// exata, case-insensitive. Lista vazia desliga o filtro geográfico (aceita qualquer UF).
    /// </summary>
    public List<string> AllowedStates { get; set; } = new() { "ES" };

    /// <summary>
    /// DDDs aceitos como fallback quando o lead não vem com UF preenchida — usados para
    /// inferir a região a partir do telefone/whatsapp.
    /// </summary>
    public List<string> AllowedDdds { get; set; } = new() { "27", "28" };

    /// <summary>
    /// Liga/desliga a checagem de MX no import do Extrator (EXTR-01). Default: true.
    /// Desligar faz todo lead ser tratado como "MX não verificado" — nada é rejeitado por MX.
    /// </summary>
    public bool MxCheckEnabled { get; set; } = true;

    /// <summary>
    /// TTL do cache de MX para resultados conclusivos (Valid / NoMx), em dias.
    /// Default 30 — paridade com CACHE_DAYS do docs/email-marketing/mx_check.py.
    /// </summary>
    public int MxCacheDays { get; set; } = 30;

    /// <summary>
    /// TTL do cache de MX para o resultado "não verificado" (timeout/falha de infraestrutura),
    /// em horas. Default 24 — muito menor que MxCacheDays porque Unverified é um fato sobre a
    /// REDE naquele momento, não sobre o domínio; congelar por 30 dias perpetuaria uma
    /// indisponibilidade momentânea.
    /// </summary>
    public int MxUnverifiedCacheHours { get; set; } = 24;

    /// <summary>
    /// Quantas queries DNS rodam em paralelo por rodada de import. Default 8 — mesmo grau de
    /// check_many(workers=8) do docs/email-marketing/site_check.py.
    /// </summary>
    public int MxLookupParallelism { get; set; } = 8;
}
