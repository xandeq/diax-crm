using Diax.Domain.EmailMarketing.Enums;
using Microsoft.Extensions.Options;

namespace Diax.Application.EmailMarketing;

/// <summary>
/// Política central de habilitação de providers de campanha.
/// Providers sem credencial válida (ex.: MailerSend/ElasticEmail) ficam em
/// Email:DisabledProviders e saem da rotação de enfileiramento, do worker e do retry.
/// </summary>
public interface IEmailProviderPolicy
{
    IReadOnlyList<EmailProvider> EnabledProviders { get; }
    bool IsEnabled(EmailProvider provider);

    /// <summary>Próximo provider habilitado após <paramref name="current"/> (rotação circular); null se nenhum.</summary>
    EmailProvider? NextEnabledAfter(EmailProvider current);

    /// <summary>Parse estrito da chave do provider ("brevo", "sendgrid", ...). Falso para nomes desconhecidos.</summary>
    bool TryParse(string? name, out EmailProvider provider);
}

public class EmailProviderPolicyOptions
{
    public const string Section = "Email";
    public List<string> DisabledProviders { get; set; } = [];
}

public class EmailProviderPolicy : IEmailProviderPolicy
{
    private static readonly (string Key, EmailProvider Provider)[] Map =
    [
        ("brevo",        EmailProvider.Brevo),
        ("mailjet",      EmailProvider.Mailjet),
        ("resend",       EmailProvider.Resend),
        ("elasticemail", EmailProvider.ElasticEmail),
        ("mailersend",   EmailProvider.MailerSend),
        ("sendgrid",     EmailProvider.SendGrid),
    ];

    private readonly IOptionsMonitor<EmailProviderPolicyOptions> _options;

    public EmailProviderPolicy(IOptionsMonitor<EmailProviderPolicyOptions> options)
    {
        _options = options;
    }

    public IReadOnlyList<EmailProvider> EnabledProviders
    {
        get
        {
            var disabled = DisabledSet();
            return Map
                .Where(m => !disabled.Contains(m.Key))
                .Select(m => m.Provider)
                .ToList();
        }
    }

    public bool IsEnabled(EmailProvider provider)
        => !DisabledSet().Contains(KeyOf(provider));

    public EmailProvider? NextEnabledAfter(EmailProvider current)
    {
        var enabled = EnabledProviders;
        if (enabled.Count == 0) return null;

        // Rotação circular a partir do provider atual, pulando o próprio quando possível.
        var ordered = Map.Select(m => m.Provider).ToList();
        var start = ordered.IndexOf(current);
        for (var offset = 1; offset <= ordered.Count; offset++)
        {
            var candidate = ordered[(start + offset) % ordered.Count];
            if (candidate != current && enabled.Contains(candidate))
                return candidate;
        }

        return enabled.Contains(current) ? current : enabled[0];
    }

    public bool TryParse(string? name, out EmailProvider provider)
    {
        provider = default;
        if (string.IsNullOrWhiteSpace(name)) return false;

        foreach (var (key, value) in Map)
        {
            if (key.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                provider = value;
                return true;
            }
        }

        return false;
    }

    public static string KeyOf(EmailProvider provider)
        => Map.First(m => m.Provider == provider).Key;

    private HashSet<string> DisabledSet()
        => new(_options.CurrentValue.DisabledProviders.Select(p => p.Trim().ToLowerInvariant()),
               StringComparer.OrdinalIgnoreCase);
}
