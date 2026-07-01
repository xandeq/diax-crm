using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Diax.Application.EmailMarketing;

public class UnsubscribeLinkBuilder : IUnsubscribeLinkBuilder
{
    private readonly IOptionsMonitor<EmailLinkOptions> _options;

    public UnsubscribeLinkBuilder(IOptionsMonitor<EmailLinkOptions> options)
    {
        _options = options;
    }

    public string DefaultCtaUrl => _options.CurrentValue.DefaultCtaUrl;

    public string BuildUnsubscribeUrl(Guid userId, string email)
    {
        var opts = _options.CurrentValue;
        var token = ComputeToken(opts.SigningKey, userId.ToString(), email);
        return $"{opts.PublicBaseUrl.TrimEnd('/')}/unsubscribe?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Token = Base64Url(HMAC-SHA256(key, "unsub:{userId}:{email}")).
    /// Algoritmo canônico — o EmailUnsubscribeController valida contra este mesmo cálculo.
    /// </summary>
    public static string ComputeToken(string key, string userId, string email)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var payload = Encoding.UTF8.GetBytes($"unsub:{userId}:{email}");
        var hash = hmac.ComputeHash(payload);
        return Convert.ToBase64String(hash)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
