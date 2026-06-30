using Diax.Application.EmailMarketing.Dispatch;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

public class ProviderQuotaGuardTests
{
    private static IProviderQuotaGuard BuildGuard(Dictionary<string, int> limits)
    {
        var options = new EmailChainOptions { ProviderDailyLimits = limits };
        var monitor = new Mock<IOptionsMonitor<EmailChainOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new ProviderQuotaGuard(monitor.Object, NullLogger<ProviderQuotaGuard>.Instance);
    }

    [Fact]
    public void TryConsume_WithinLimit_ReturnsTrue()
    {
        var guard = BuildGuard(new() { ["brevo"] = 5 });
        Assert.True(guard.TryConsume("brevo"));
    }

    [Fact]
    public void TryConsume_ExceedsLimit_ReturnsFalse()
    {
        var guard = BuildGuard(new() { ["brevo"] = 2 });
        Assert.True(guard.TryConsume("brevo"));  // 1
        Assert.True(guard.TryConsume("brevo"));  // 2
        Assert.False(guard.TryConsume("brevo")); // 3 — bloqueado
    }

    [Fact]
    public void TryConsume_UnknownProvider_ReturnsTrue()
    {
        // Providers não listados não têm limite (free pass)
        var guard = BuildGuard(new());
        Assert.True(guard.TryConsume("any-provider"));
    }

    [Fact]
    public void GetRemaining_StartsAtLimit()
    {
        var guard = BuildGuard(new() { ["sendgrid"] = 90 });
        Assert.Equal(90, guard.GetRemaining("sendgrid"));
    }

    [Fact]
    public void GetRemaining_DecreasesAfterConsume()
    {
        var guard = BuildGuard(new() { ["sendgrid"] = 90 });
        guard.TryConsume("sendgrid");
        guard.TryConsume("sendgrid");
        Assert.Equal(88, guard.GetRemaining("sendgrid"));
    }

    [Fact]
    public void GetRemaining_NeverBelowZero()
    {
        var guard = BuildGuard(new() { ["resend"] = 2 });
        guard.TryConsume("resend");
        guard.TryConsume("resend");
        guard.TryConsume("resend"); // bloqueado, não consome
        Assert.Equal(0, guard.GetRemaining("resend"));
    }

    [Fact]
    public void GetRemaining_UnknownProvider_ReturnsMaxValue()
    {
        var guard = BuildGuard(new());
        Assert.Equal(int.MaxValue, guard.GetRemaining("unknown"));
    }

    [Fact]
    public void GetStatus_ReflectsCurrentUsage()
    {
        var guard = BuildGuard(new() { ["brevo"] = 280, ["mailjet"] = 180 });
        guard.TryConsume("brevo");
        guard.TryConsume("brevo");

        var status = guard.GetStatus();
        Assert.Equal(2, status["brevo"].Used);
        Assert.Equal(278, status["brevo"].Remaining);
        Assert.Equal(0, status["mailjet"].Used);
        Assert.Equal(180, status["mailjet"].Remaining);
    }

    [Fact]
    public void TryConsume_IsCaseSensitive_ByKeyComparison()
    {
        // Keys de provider são lowercase por convenção — verificar que maiúsculas não criam slots separados
        var guard = BuildGuard(new() { ["brevo"] = 2 });
        Assert.True(guard.TryConsume("brevo"));
        Assert.True(guard.TryConsume("brevo"));
        Assert.False(guard.TryConsume("brevo")); // esgotado
        // "Brevo" (maiúscula) deve usar o mesmo slot (OrdinalIgnoreCase)
        Assert.False(guard.TryConsume("Brevo"));
    }

    [Fact]
    public void MultipleProviders_IndependentLimits()
    {
        var guard = BuildGuard(new() { ["brevo"] = 1, ["mailjet"] = 3 });
        Assert.True(guard.TryConsume("brevo"));
        Assert.False(guard.TryConsume("brevo")); // brevo esgotado
        Assert.True(guard.TryConsume("mailjet")); // mailjet ainda disponível
        Assert.True(guard.TryConsume("mailjet"));
    }
}
