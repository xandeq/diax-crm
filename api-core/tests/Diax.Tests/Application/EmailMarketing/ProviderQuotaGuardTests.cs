using Diax.Application.EmailMarketing.Dispatch;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

public class ProviderQuotaGuardTests
{
    /// <summary>Fonte de uso fake — devolve um valor fixo por provider (default 0).</summary>
    private sealed class StubUsageSource : IProviderQuotaUsageSource
    {
        private readonly Dictionary<string, int> _used;
        public int Calls { get; private set; }

        public StubUsageSource(Dictionary<string, int>? used = null)
            => _used = used ?? new(StringComparer.OrdinalIgnoreCase);

        public Task<int> GetUsedSinceAsync(string providerKey, DateTime sinceUtc, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(_used.TryGetValue(providerKey, out var v) ? v : 0);
        }
    }

    private static IProviderQuotaGuard BuildGuard(
        Dictionary<string, int>? dailyLimits = null,
        Dictionary<string, int>? weeklyLimits = null,
        IProviderQuotaUsageSource? usageSource = null)
    {
        var options = new EmailChainOptions
        {
            ProviderDailyLimits  = dailyLimits  ?? new(),
            ProviderWeeklyLimits = weeklyLimits ?? new()
        };
        var monitor = new Mock<IOptionsMonitor<EmailChainOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new ProviderQuotaGuard(
            monitor.Object,
            usageSource ?? new StubUsageSource(),
            NullLogger<ProviderQuotaGuard>.Instance);
    }

    // ───── Daily quota ─────

    [Fact]
    public async Task TryConsume_WithinDailyLimit_ReturnsTrue()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 5 });
        Assert.True(await guard.TryConsumeAsync("brevo"));
    }

    [Fact]
    public async Task TryConsume_ExceedsDailyLimit_ReturnsFalse()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 2 });
        Assert.True(await guard.TryConsumeAsync("brevo"));   // 1
        Assert.True(await guard.TryConsumeAsync("brevo"));   // 2
        Assert.False(await guard.TryConsumeAsync("brevo"));  // bloqueado
    }

    [Fact]
    public async Task TryConsume_UnknownProvider_ReturnsTrue()
    {
        var guard = BuildGuard();
        Assert.True(await guard.TryConsumeAsync("any-provider"));
    }

    [Fact]
    public async Task GetRemaining_StartsAtDailyLimit()
    {
        var guard = BuildGuard(dailyLimits: new() { ["sendgrid"] = 90 });
        Assert.Equal(90, await guard.GetRemainingAsync("sendgrid"));
    }

    [Fact]
    public async Task GetRemaining_DecreasesAfterConsume()
    {
        var guard = BuildGuard(dailyLimits: new() { ["sendgrid"] = 90 });
        await guard.TryConsumeAsync("sendgrid");
        await guard.TryConsumeAsync("sendgrid");
        Assert.Equal(88, await guard.GetRemainingAsync("sendgrid"));
    }

    [Fact]
    public async Task GetRemaining_NeverBelowZero()
    {
        var guard = BuildGuard(dailyLimits: new() { ["resend"] = 2 });
        await guard.TryConsumeAsync("resend");
        await guard.TryConsumeAsync("resend");
        await guard.TryConsumeAsync("resend"); // bloqueado, não consome
        Assert.Equal(0, await guard.GetRemainingAsync("resend"));
    }

    [Fact]
    public async Task GetRemaining_UnknownProvider_ReturnsMaxValue()
    {
        var guard = BuildGuard();
        Assert.Equal(int.MaxValue, await guard.GetRemainingAsync("unknown"));
    }

    [Fact]
    public async Task GetStatus_ReflectsDailyUsage()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 280, ["mailjet"] = 180 });
        await guard.TryConsumeAsync("brevo");
        await guard.TryConsumeAsync("brevo");

        var status = await guard.GetStatusAsync();
        Assert.Equal(2,   status["brevo"].DailyUsed);
        Assert.Equal(278, status["brevo"].DailyRemaining);
        Assert.Equal(0,   status["mailjet"].DailyUsed);
        Assert.Equal(180, status["mailjet"].DailyRemaining);
    }

    [Fact]
    public async Task TryConsume_CaseInsensitive_SameSlot()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 2 });
        Assert.True(await guard.TryConsumeAsync("brevo"));
        Assert.True(await guard.TryConsumeAsync("brevo"));
        Assert.False(await guard.TryConsumeAsync("brevo")); // esgotado
        Assert.False(await guard.TryConsumeAsync("Brevo")); // mesmo slot (case-insensitive)
    }

    [Fact]
    public async Task MultipleProviders_IndependentDailyLimits()
    {
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 1, ["mailjet"] = 3 });
        Assert.True(await guard.TryConsumeAsync("brevo"));
        Assert.False(await guard.TryConsumeAsync("brevo"));   // brevo esgotado
        Assert.True(await guard.TryConsumeAsync("mailjet"));  // mailjet ainda disponível
        Assert.True(await guard.TryConsumeAsync("mailjet"));
    }

    // ───── Weekly quota ─────

    [Fact]
    public async Task TryConsume_WithinWeeklyLimit_ReturnsTrue()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2000 });
        Assert.True(await guard.TryConsumeAsync("gmail-smtp"));
    }

    [Fact]
    public async Task TryConsume_ExceedsWeeklyLimit_ReturnsFalse()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2 });
        Assert.True(await guard.TryConsumeAsync("gmail-smtp"));   // 1
        Assert.True(await guard.TryConsumeAsync("gmail-smtp"));   // 2
        Assert.False(await guard.TryConsumeAsync("gmail-smtp"));  // bloqueado por semanal
    }

    [Fact]
    public async Task TryConsume_WeeklyBlocksEvenWhenDailyFree()
    {
        // Daily = 100, Weekly = 2 — semanal deve bloquear antes
        var guard = BuildGuard(
            dailyLimits:  new() { ["gmail-smtp"] = 100 },
            weeklyLimits: new() { ["gmail-smtp"] = 2 });
        Assert.True(await guard.TryConsumeAsync("gmail-smtp"));   // 1
        Assert.True(await guard.TryConsumeAsync("gmail-smtp"));   // 2
        Assert.False(await guard.TryConsumeAsync("gmail-smtp"));  // semanal esgotado → bloqueia
    }

    [Fact]
    public async Task GetStatus_ReflectsWeeklyUsage()
    {
        var guard = BuildGuard(weeklyLimits: new() { ["gmail-smtp"] = 2000 });
        await guard.TryConsumeAsync("gmail-smtp");
        await guard.TryConsumeAsync("gmail-smtp");
        await guard.TryConsumeAsync("gmail-smtp");

        var status = await guard.GetStatusAsync();
        Assert.Equal(3,    status["gmail-smtp"].WeeklyUsed);
        Assert.Equal(2000, status["gmail-smtp"].WeeklyLimit);
        Assert.Equal(1997, status["gmail-smtp"].WeeklyRemaining);
    }

    [Fact]
    public async Task GetRemaining_ReturnsMinOfDailyAndWeekly()
    {
        // daily=10, weekly=3 → min é 3
        var guard = BuildGuard(
            dailyLimits:  new() { ["provider"] = 10 },
            weeklyLimits: new() { ["provider"] = 3 });
        Assert.Equal(3, await guard.GetRemainingAsync("provider"));
    }

    // ───── Hidratação do banco (sobrevive a recycle) ─────

    [Fact]
    public async Task TryConsume_HydratesFromDatabase_OnColdStart()
    {
        // Simula restart do processo com 3 envios já feitos hoje (registrados no banco):
        // a quota in-memory começa em 3, não em 0 — só sobra 1 crédito de um limite de 4.
        var guard = BuildGuard(
            dailyLimits: new() { ["brevo"] = 4 },
            usageSource: new StubUsageSource(new() { ["brevo"] = 3 }));

        Assert.True(await guard.TryConsumeAsync("brevo"));   // 4/4
        Assert.False(await guard.TryConsumeAsync("brevo"));  // limite atingido
    }

    [Fact]
    public async Task TryConsume_HydrationFailure_FallsBackToInMemory()
    {
        var failingSource = new Mock<IProviderQuotaUsageSource>();
        failingSource
            .Setup(s => s.GetUsedSinceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var guard = BuildGuard(
            dailyLimits: new() { ["brevo"] = 2 },
            usageSource: failingSource.Object);

        // Falha de hidratação não pode derrubar envio — comportamento in-memory puro.
        Assert.True(await guard.TryConsumeAsync("brevo"));
        Assert.True(await guard.TryConsumeAsync("brevo"));
        Assert.False(await guard.TryConsumeAsync("brevo"));
    }

    [Fact]
    public async Task TryConsume_HydratesOnlyOnce_PerPeriod()
    {
        var stub = new StubUsageSource(new() { ["brevo"] = 1 });
        var guard = BuildGuard(dailyLimits: new() { ["brevo"] = 10 }, usageSource: stub);

        await guard.TryConsumeAsync("brevo");
        await guard.TryConsumeAsync("brevo");
        await guard.TryConsumeAsync("brevo");

        Assert.Equal(1, stub.Calls); // hidratou uma única vez
        Assert.Equal(6, await guard.GetRemainingAsync("brevo")); // 10 - (1 hidratado + 3 consumidos)
    }
}
