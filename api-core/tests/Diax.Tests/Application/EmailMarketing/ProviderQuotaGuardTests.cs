using Diax.Application.EmailMarketing.Dispatch;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

public class ProviderQuotaGuardTests
{
    private static IProviderQuotaGuard BuildGuard(
        Dictionary<string, int>? dailyLimits = null,
        Dictionary<string, int>? weeklyLimits = null)
    {
        var options = new EmailChainOptions
        {
            ProviderDailyLimits  = dailyLimits  ?? new(),
            ProviderWeeklyLimits = weeklyLimits ?? new()
        };
        var monitor = new Mock<IOptionsMonitor<EmailChainOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new ProviderQuotaGuard(monitor.Object, NullLogger<ProviderQuotaGuard>.Instance);
    }

    // ───── Daily quota ─────

    [Fact]
    public void TryConsume_WithinDailyLimit_ReturnsTrue()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 5 });
        Assert.True(guard.TryConsume("brevo"));
    }

    [Fact]
    public void TryConsume_ExceedsDailyLimit_ReturnsFalse()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 2 });
        Assert.True(guard.TryConsume("brevo"));   // 1
        Assert.True(guard.TryConsume("brevo"));   // 2
        Assert.False(guard.TryConsume("brevo"));  // bloqueado
    }

    [Fact]
    public void TryConsume_UnknownProvider_ReturnsTrue()
    {
        var guard = BuildGuard();
        Assert.True(guard.TryConsume("any-provider"));
    }

    [Fact]
    public void GetRemaining_StartsAtDailyLimit()
    {
        var guard = BuildGuard(dailyLimits: new() { ["sendgrid"] = 90 });
        Assert.Equal(90, guard.GetRemaining("sendgrid"));
    }

    [Fact]
    public void GetRemaining_DecreasesAfterConsume()
    {
        var guard = BuildGuard(dailyLimits: new() { ["sendgrid"] = 90 });
        guard.TryConsume("sendgrid");
        guard.TryConsume("sendgrid");
        Assert.Equal(88, guard.GetRemaining("sendgrid"));
    }

    [Fact]
    public void GetRemaining_NeverBelowZero()
    {
        var guard = BuildGuard(dailyLimits: new() { ["resend"] = 2 });
        guard.TryConsume("resend");
        guard.TryConsume("resend");
        guard.TryConsume("resend"); // bloqueado, não consome
        Assert.Equal(0, guard.GetRemaining("resend"));
    }

    [Fact]
    public void GetRemaining_UnknownProvider_ReturnsMaxValue()
    {
        var guard = BuildGuard();
        Assert.Equal(int.MaxValue, guard.GetRemaining("unknown"));
    }

    [Fact]
    public void GetStatus_ReflectsDailyUsage()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 280, ["mailjet"] = 180 });
        guard.TryConsume("brevo");
        guard.TryConsume("brevo");

        var status = guard.GetStatus();
        Assert.Equal(2,   status["brevo"].DailyUsed);
        Assert.Equal(278, status["brevo"].DailyRemaining);
        Assert.Equal(0,   status["mailjet"].DailyUsed);
        Assert.Equal(180, status["mailjet"].DailyRemaining);
    }

    [Fact]
    public void TryConsume_CaseInsensitive_SameSlot()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 2 });
        Assert.True(guard.TryConsume("brevo"));
        Assert.True(guard.TryConsume("brevo"));
        Assert.False(guard.TryConsume("brevo")); // esgotado
        Assert.False(guard.TryConsume("Brevo")); // mesmo slot (case-insensitive)
    }

    [Fact]
    public void MultipleProviders_IndependentDailyLimits()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 1, ["mailjet"] = 3 });
        Assert.True(guard.TryConsume("brevo"));
        Assert.False(guard.TryConsume("brevo"));   // brevo esgotado
        Assert.True(guard.TryConsume("mailjet"));  // mailjet ainda disponível
        Assert.True(guard.TryConsume("mailjet"));
    }

    // ───── Weekly quota ─────

    [Fact]
    public void TryConsume_WithinWeeklyLimit_ReturnsTrue()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2000 });
        Assert.True(guard.TryConsume("gmail-smtp"));
    }

    [Fact]
    public void TryConsume_ExceedsWeeklyLimit_ReturnsFalse()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2 });
        Assert.True(guard.TryConsume("gmail-smtp"));   // 1
        Assert.True(guard.TryConsume("gmail-smtp"));   // 2
        Assert.False(guard.TryConsume("gmail-smtp"));  // bloqueado por semanal
    }

    [Fact]
    public void TryConsume_WeeklyBlocksEvenWhenDailyFree()
    {
        // Daily = 100, Weekly = 2 — semanal deve bloquear antes
        var guard = BuildGuard(
            dailyLimits:  new() { ["gmail-smtp"] = 100 },
            weeklyLimits: new() { ["gmail-smtp"] = 2 });
        Assert.True(guard.TryConsume("gmail-smtp"));   // 1
        Assert.True(guard.TryConsume("gmail-smtp"));   // 2
        Assert.False(guard.TryConsume("gmail-smtp"));  // semanal esgotado → bloqueia
    }

    [Fact]
    public void GetStatus_ReflectsWeeklyUsage()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2000 });
        guard.TryConsume("gmail-smtp");
        guard.TryConsume("gmail-smtp");
        guard.TryConsume("gmail-smtp");

        var status = guard.GetStatus();
        Assert.Equal(3,    status["gmail-smtp"].WeeklyUsed);
        Assert.Equal(2000, status["gmail-smtp"].WeeklyLimit);
        Assert.Equal(1997, status["gmail-smtp"].WeeklyRemaining);
    }

    [Fact]
    public void GetRemaining_ReturnsMinOfDailyAndWeekly()
    {
        // daily=10, weekly=3 → min é 3
        var guard = BuildGuard(
            dailyLimits:  new() { ["provider"] = 10 },
            weeklyLimits: new() { ["provider"] = 3 });
        Assert.Equal(3, guard.GetRemaining("provider"));
    }
}
