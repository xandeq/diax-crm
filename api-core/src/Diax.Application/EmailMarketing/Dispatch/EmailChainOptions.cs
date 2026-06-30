namespace Diax.Application.EmailMarketing.Dispatch;

public class EmailChainOptions
{
    public const string Section = "EmailChain";
    public TimeSpan HardTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public Dictionary<string, SenderDomainConfig> SenderDomains { get; set; } = new();
}

public class SenderDomainConfig
{
    /// <summary>ESPs com DKIM alinhado ao domínio remetente (Tier 1 — preferencial).</summary>
    public List<string> Tier1Providers { get; set; } = [];

    /// <summary>Servidores SMTP próprios (Tier 2 — só usados se allowUnaligned=true).</summary>
    public List<string> Tier2Providers { get; set; } = [];
}
