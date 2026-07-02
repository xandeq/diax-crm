using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Domain.EmailMarketing;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Testes do EmailFallbackOrchestrator REAL (não do mock da interface) — cobre a lacuna
/// apontada na auditoria: fallback A→B, gate Tier2, skip por breaker/quota, idempotência
/// concorrente, timeout por provider vs. timeout da cadeia (Uncertain) e ProviderHint.
/// </summary>
public class EmailFallbackOrchestratorTests
{
    private const string Domain = "test.local";

    // ───── fakes ─────

    private sealed class FakeSender : IEmailSender
    {
        private readonly Func<CancellationToken, Task<EmailSendResult>> _behavior;
        private int _calls;
        public int Calls => _calls;

        private FakeSender(Func<CancellationToken, Task<EmailSendResult>> behavior) => _behavior = behavior;

        public Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailSendResult.Fail("overload de campanha não usado no dispatch unificado"));

        public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return await _behavior(cancellationToken);
        }

        public static FakeSender Ok(string messageId) => new(_ => Task.FromResult(EmailSendResult.Ok(messageId)));
        public static FakeSender Failing(string error) => new(_ => Task.FromResult(EmailSendResult.Fail(error)));
        public static FakeSender Hanging() => new(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return EmailSendResult.Ok("nunca");
        });
        public static FakeSender OkAfter(TimeSpan delay, string messageId) => new(async ct =>
        {
            await Task.Delay(delay, ct);
            return EmailSendResult.Ok(messageId);
        });
    }

    /// <summary>
    /// Repositório in-memory que emula o índice único filtrado de idempotency key
    /// (um registro InFlight/Sent por chave), como o SQL Server faz em produção.
    /// </summary>
    private sealed class InMemoryEmailSendLogRepository : IEmailSendLogRepository
    {
        private readonly object _lock = new();
        public readonly List<EmailSendLog> Logs = [];

        public Task<EmailSendLog?> FindRecentByIdempotencyKeyAsync(string idempotencyKey, TimeSpan window, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - window;
                return Task.FromResult(Logs
                    .Where(l => l.IdempotencyKey == idempotencyKey && l.CreatedAt >= cutoff)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefault());
            }
        }

        public Task<EmailSendLog?> TryCreateInFlightAsync(
            string requestId, string? idempotencyKey, string toHash, string subjectHash,
            string? bodyHash, string fromDomain, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (idempotencyKey is not null &&
                    Logs.Any(l => l.IdempotencyKey == idempotencyKey && l.Status is "InFlight" or "Sent"))
                {
                    return Task.FromResult<EmailSendLog?>(null);
                }

                var log = EmailSendLog.CreateInFlight(requestId, idempotencyKey, toHash, subjectHash, bodyHash, fromDomain);
                Logs.Add(log);
                return Task.FromResult<EmailSendLog?>(log);
            }
        }

        public Task RecordAttemptAsync(Guid logId, string provider, int attemptNo, bool success,
            string? providerMessageId, string? error, int latencyMs, bool allowUnaligned, CancellationToken ct = default)
        {
            lock (_lock)
            {
                Logs.FirstOrDefault(l => l.Id == logId)
                    ?.RecordAttempt(provider, attemptNo, success, providerMessageId, error, latencyMs, allowUnaligned);
            }
            return Task.CompletedTask;
        }

        public Task MarkSentAsync(Guid logId, string provider, string? providerMessageId, bool allowUnaligned, CancellationToken ct = default)
        {
            lock (_lock) { Logs.FirstOrDefault(l => l.Id == logId)?.MarkSent(provider, providerMessageId, allowUnaligned); }
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid logId, CancellationToken ct = default)
        {
            lock (_lock) { Logs.FirstOrDefault(l => l.Id == logId)?.MarkFailed(); }
            return Task.CompletedTask;
        }

        public Task MarkUncertainAsync(Guid logId, string? reason, CancellationToken ct = default)
        {
            lock (_lock) { Logs.FirstOrDefault(l => l.Id == logId)?.MarkUncertain(reason); }
            return Task.CompletedTask;
        }
    }

    // ───── harness ─────

    private sealed class Harness
    {
        public InMemoryEmailSendLogRepository LogRepo { get; } = new();
        public EmailProviderCircuitBreaker Breaker { get; } = new(TimeSpan.FromMinutes(5));
        public ProviderQuotaGuard Quota { get; }
        public EmailChainOptions Options { get; }
        public EmailFallbackOrchestrator Orchestrator { get; }

        public Harness(
            Dictionary<string, IEmailSender> senders,
            List<string>? tier1 = null,
            List<string>? tier2 = null,
            TimeSpan? hardTimeout = null,
            TimeSpan? perProviderTimeout = null,
            Dictionary<string, int>? dailyLimits = null)
        {
            Options = new EmailChainOptions
            {
                HardTimeout = hardTimeout ?? TimeSpan.FromSeconds(10),
                PerProviderTimeout = perProviderTimeout ?? TimeSpan.FromSeconds(5),
                InFlightStaleAfter = TimeSpan.FromMinutes(10),
                ProviderDailyLimits = dailyLimits ?? new(),
                SenderDomains = new Dictionary<string, SenderDomainConfig>
                {
                    [Domain] = new()
                    {
                        Tier1Providers = tier1 ?? [.. senders.Keys],
                        Tier2Providers = tier2 ?? []
                    }
                }
            };

            var monitor = new Mock<IOptionsMonitor<EmailChainOptions>>();
            monitor.Setup(m => m.CurrentValue).Returns(Options);

            var services = new ServiceCollection();
            foreach (var (key, sender) in senders)
            {
                services.AddKeyedSingleton(key, sender);
            }

            Quota = new ProviderQuotaGuard(
                monitor.Object,
                new Mock<IProviderQuotaUsageSource>().Object,
                NullLogger<ProviderQuotaGuard>.Instance);

            Orchestrator = new EmailFallbackOrchestrator(
                services.BuildServiceProvider(),
                monitor.Object,
                Breaker,
                Quota,
                LogRepo,
                NullLogger<EmailFallbackOrchestrator>.Instance);
        }
    }

    private static EmailDispatchRequest Request(
        string? idempotencyKey = null,
        string? providerHint = null,
        bool allowUnaligned = false)
    {
        return new EmailDispatchRequest(
            new EmailMessage
            {
                From = new EmailAddress($"noreply@{Domain}"),
                To = [new EmailAddress("dest@example.com")],
                Subject = "Assunto",
                Html = "<p>corpo</p>"
            },
            idempotencyKey,
            providerHint,
            Guid.NewGuid().ToString("N"),
            allowUnaligned);
    }

    // ───── fallback real ─────

    [Fact]
    public async Task Dispatch_ProviderAFails_FallsBackToB()
    {
        var a = FakeSender.Failing("erro 500 interno");
        var b = FakeSender.Ok("msg-b");
        var h = new Harness(new() { ["a"] = a, ["b"] = b }, tier1: ["a", "b"]);

        var result = await h.Orchestrator.DispatchAsync(Request());

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("b", result.ProviderUsed);
        Assert.Equal(2, result.Attempts.Count);
        Assert.Equal(1, a.Calls);
        Assert.Equal(1, b.Calls);
        Assert.Equal("Sent", h.LogRepo.Logs.Single().Status);
    }

    [Fact]
    public async Task Dispatch_AllTier1Fail_WithoutAllowUnaligned_NeverTouchesTier2()
    {
        var a = FakeSender.Failing("falha a");
        var b = FakeSender.Failing("falha b");
        var t2 = FakeSender.Ok("msg-t2");
        var h = new Harness(new() { ["a"] = a, ["b"] = b, ["t2-smtp"] = t2 },
            tier1: ["a", "b"], tier2: ["t2-smtp"]);

        var result = await h.Orchestrator.DispatchAsync(Request(allowUnaligned: false));

        Assert.Equal(EmailDispatchStatus.AllFailed, result.Status);
        Assert.Equal(0, t2.Calls);
        Assert.Equal("Failed", h.LogRepo.Logs.Single().Status);
    }

    [Fact]
    public async Task Dispatch_AllowUnaligned_UsesTier2AndFlagsResult()
    {
        var a = FakeSender.Failing("falha a");
        var t2 = FakeSender.Ok("msg-t2");
        var h = new Harness(new() { ["a"] = a, ["t2-smtp"] = t2 },
            tier1: ["a"], tier2: ["t2-smtp"]);

        var result = await h.Orchestrator.DispatchAsync(Request(allowUnaligned: true));

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("t2-smtp", result.ProviderUsed);
        Assert.True(result.AllowUnaligned);
    }

    [Fact]
    public async Task Dispatch_BreakerOpenForA_SkipsDirectlyToB()
    {
        var a = FakeSender.Ok("msg-a");
        var b = FakeSender.Ok("msg-b");
        var h = new Harness(new() { ["a"] = a, ["b"] = b }, tier1: ["a", "b"]);

        h.Breaker.RecordFailure("a", "401 invalid api key"); // abre o breaker de A na hora

        var result = await h.Orchestrator.DispatchAsync(Request());

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("b", result.ProviderUsed);
        Assert.Equal(0, a.Calls); // A nunca foi chamado
    }

    [Fact]
    public async Task Dispatch_QuotaExhaustedForA_SkipsToB()
    {
        var a = FakeSender.Ok("msg-a");
        var b = FakeSender.Ok("msg-b");
        var h = new Harness(new() { ["a"] = a, ["b"] = b },
            tier1: ["a", "b"], dailyLimits: new() { ["a"] = 1 });

        Assert.True(await h.Quota.TryConsumeAsync("a")); // esgota a quota de A

        var result = await h.Orchestrator.DispatchAsync(Request());

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("b", result.ProviderUsed);
        Assert.Equal(0, a.Calls);
    }

    // ───── timeouts ─────

    [Fact]
    public async Task Dispatch_ProviderTimeout_ContinuesToNextProvider()
    {
        // Regressão do P1-9: antes, UM provider pendurado abortava a cadeia inteira.
        var a = FakeSender.Hanging();
        var b = FakeSender.Ok("msg-b");
        var h = new Harness(new() { ["a"] = a, ["b"] = b },
            tier1: ["a", "b"],
            hardTimeout: TimeSpan.FromSeconds(10),
            perProviderTimeout: TimeSpan.FromMilliseconds(150));

        var result = await h.Orchestrator.DispatchAsync(Request());

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("b", result.ProviderUsed);
        Assert.Equal(2, result.Attempts.Count);
        Assert.False(result.Attempts[0].Success);
        Assert.Contains("Timeout do provider", result.Attempts[0].Error);
    }

    [Fact]
    public async Task Dispatch_HardTimeoutMidSend_ReturnsUncertain_NotFailed()
    {
        // Resultado ambíguo: o provider pode ter aceitado — marcar Failed levaria o
        // caller a re-enviar com chave nova e duplicar o email.
        var a = FakeSender.Hanging();
        var h = new Harness(new() { ["a"] = a },
            tier1: ["a"],
            hardTimeout: TimeSpan.FromMilliseconds(150),
            perProviderTimeout: TimeSpan.FromSeconds(30));

        var result = await h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k-uncertain"));

        Assert.Equal(EmailDispatchStatus.Uncertain, result.Status);
        Assert.Equal("Uncertain", h.LogRepo.Logs.Single().Status);
    }

    // ───── idempotência ─────

    [Fact]
    public async Task Dispatch_IdempotentReplay_AfterSent_ReturnsDuplicateWithoutSending()
    {
        var a = FakeSender.Ok("msg-a");
        var h = new Harness(new() { ["a"] = a }, tier1: ["a"]);

        var first = await h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k1"));
        var replay = await h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k1"));

        Assert.Equal(EmailDispatchStatus.Sent, first.Status);
        Assert.Equal(EmailDispatchStatus.Duplicate, replay.Status);
        Assert.Equal("msg-a", replay.MessageId);
        Assert.Equal(1, a.Calls); // enviou UMA vez
    }

    [Fact]
    public async Task Dispatch_ConcurrentSameKey_ExactlyOneSends()
    {
        // Regressão do P0-6 (TOCTOU): duas chamadas simultâneas com a mesma chave
        // não podem enviar duas vezes — o índice único (emulado no fake) decide.
        var a = FakeSender.OkAfter(TimeSpan.FromMilliseconds(150), "msg-a");
        var h = new Harness(new() { ["a"] = a }, tier1: ["a"]);

        var results = await Task.WhenAll(
            h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k-race")),
            h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k-race")));

        Assert.Equal(1, a.Calls);
        Assert.Single(results, r => r.Status == EmailDispatchStatus.Sent);
        Assert.Single(results, r => r.Status is EmailDispatchStatus.InProgress or EmailDispatchStatus.Duplicate);
    }

    [Fact]
    public async Task Dispatch_StaleInFlight_IsMarkedUncertainAndKeyIsReleased()
    {
        // Regressão do P0-6: InFlight órfão (crash) bloqueava a chave por 24h com 409.
        var a = FakeSender.Ok("msg-a");
        var h = new Harness(new() { ["a"] = a }, tier1: ["a"]);

        // Simula um InFlight órfão de 30 minutos atrás (crash durante dispatch anterior)
        var orphan = await h.LogRepo.TryCreateInFlightAsync("req-old", "k-stale", "h", "h", "h", Domain);
        typeof(Diax.Domain.Common.AuditableEntity)
            .GetProperty("CreatedAt")!
            .SetValue(orphan, DateTime.UtcNow.AddMinutes(-30));

        var result = await h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k-stale"));

        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal(1, a.Calls);
        Assert.Contains(h.LogRepo.Logs, l => l.Status == "Uncertain"); // órfão marcado
        Assert.Contains(h.LogRepo.Logs, l => l.Status == "Sent");      // novo dispatch ok
    }

    [Fact]
    public async Task Dispatch_FreshInFlight_ReturnsInProgress()
    {
        var a = FakeSender.Ok("msg-a");
        var h = new Harness(new() { ["a"] = a }, tier1: ["a"]);

        await h.LogRepo.TryCreateInFlightAsync("req-1", "k-fresh", "h", "h", "h", Domain);

        var result = await h.Orchestrator.DispatchAsync(Request(idempotencyKey: "k-fresh"));

        Assert.Equal(EmailDispatchStatus.InProgress, result.Status);
        Assert.Equal(0, a.Calls);
    }

    // ───── ProviderHint (antes era parâmetro morto) ─────

    [Fact]
    public async Task Dispatch_ProviderHint_MovesProviderToFrontOfItsTier()
    {
        var a = FakeSender.Ok("msg-a");
        var b = FakeSender.Ok("msg-b");
        var h = new Harness(new() { ["a"] = a, ["b"] = b }, tier1: ["a", "b"]);

        var result = await h.Orchestrator.DispatchAsync(Request(providerHint: "b"));

        Assert.Equal("b", result.ProviderUsed);
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public async Task Dispatch_ProviderHintOnTier2_WithoutAllowUnaligned_IsNotPromoted()
    {
        var a = FakeSender.Ok("msg-a");
        var t2 = FakeSender.Ok("msg-t2");
        var h = new Harness(new() { ["a"] = a, ["t2-smtp"] = t2 },
            tier1: ["a"], tier2: ["t2-smtp"]);

        var result = await h.Orchestrator.DispatchAsync(Request(providerHint: "t2-smtp", allowUnaligned: false));

        Assert.Equal("a", result.ProviderUsed);
        Assert.Equal(0, t2.Calls);
    }

    [Fact]
    public async Task Dispatch_UnknownSenderDomain_IsRejected()
    {
        var a = FakeSender.Ok("msg-a");
        var h = new Harness(new() { ["a"] = a }, tier1: ["a"]);

        var request = new EmailDispatchRequest(
            new EmailMessage
            {
                From = new EmailAddress("x@dominio-desconhecido.com"),
                To = [new EmailAddress("dest@example.com")],
                Subject = "s",
                Html = "<p>x</p>"
            },
            null, null, Guid.NewGuid().ToString("N"), false);

        var result = await h.Orchestrator.DispatchAsync(request);

        Assert.Equal(EmailDispatchStatus.Rejected, result.Status);
        Assert.Equal(0, a.Calls);
    }
}
