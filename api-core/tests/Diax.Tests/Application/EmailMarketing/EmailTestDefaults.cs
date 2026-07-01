using Diax.Application.EmailMarketing;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Implementações reais (não-mock) de IUnsubscribeLinkBuilder e IEmailProviderPolicy
/// com configuração de teste — compartilhadas pelos testes do módulo de email.
/// </summary>
internal static class EmailTestDefaults
{
    public const string PublicBaseUrl = "https://api.test.local";
    public const string CtaUrl = "https://cta.test.local";
    public const string SigningKey = "test-signing-key";

    public static IUnsubscribeLinkBuilder LinkBuilder()
    {
        var options = new EmailLinkOptions
        {
            PublicBaseUrl = PublicBaseUrl,
            DefaultCtaUrl = CtaUrl,
            SigningKey = SigningKey
        };
        var monitor = new Mock<IOptionsMonitor<EmailLinkOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new UnsubscribeLinkBuilder(monitor.Object);
    }

    public static IEmailProviderPolicy ProviderPolicy(params string[] disabledProviders)
    {
        var options = new EmailProviderPolicyOptions
        {
            DisabledProviders = disabledProviders.ToList()
        };
        var monitor = new Mock<IOptionsMonitor<EmailProviderPolicyOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new EmailProviderPolicy(monitor.Object);
    }
}
