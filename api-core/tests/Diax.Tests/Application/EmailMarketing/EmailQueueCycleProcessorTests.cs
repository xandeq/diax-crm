using System.Linq.Expressions;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Ciclo de despacho da fila (antes intestável dentro do BackgroundService).
/// Cobre P0-3 (recovery de Processing órfão), P1-7 (reassinalação de provider no retry
/// + resgate de providers desabilitados), P1-8 (parada imediata quando o breaker global
/// abre no meio do lote), P1-12 (DecrementFailed no requeue) e sandbox mode.
/// </summary>
public class EmailQueueCycleProcessorTests
{
    private sealed class QueueFakeSender : IEmailSender
    {
        public readonly List<EmailSendMessage> Sent = [];
        private readonly Func<EmailSendMessage, EmailSendResult> _behavior;

        public QueueFakeSender(Func<EmailSendMessage, EmailSendResult>? behavior = null)
            => _behavior = behavior ?? (_ => EmailSendResult.Ok("q-msg"));

        public Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(_behavior(message));
        }
    }

    private sealed class RecordingAlerter : IEmailOpsAlerter
    {
        public readonly List<(string Key, string Message)> Alerts = [];

        public Task NotifyAsync(string throttleKey, string htmlMessage, CancellationToken cancellationToken = default)
        {
            Alerts.Add((throttleKey, htmlMessage));
            return Task.CompletedTask;
        }
    }

    private sealed class Harness
    {
        public Mock<IEmailQueueRepository> QueueRepo { get; } = new();
        public Mock<IEmailCampaignRepository> CampaignRepo { get; } = new();
        public Mock<ICustomerRepository> CustomerRepo { get; } = new();
        public Mock<IEmailSuppressionRepository> SuppressionRepo { get; } = new();
        public PilotCircuitBreaker PilotBreaker { get; } = new();
        public EmailProviderCircuitBreaker ProviderBreaker { get; } = new(TimeSpan.FromMinutes(5));
        public QueueFakeSender Sender { get; }
        public RecordingAlerter Alerter { get; } = new();
        public EmailSettings Settings { get; }
        public EmailQueueCycleProcessor Processor { get; }

        public Harness(
            QueueFakeSender? sender = null,
            string[]? disabledProviders = null,
            string? sandboxRedirectTo = null,
            Dictionary<string, int>? providerDailyLimits = null,
            bool inCycleFallback = false,
            int maxFallbackProviders = 2,
            int batchSize = 50)
        {
            Sender = sender ?? new QueueFakeSender();
            Settings = new EmailSettings
            {
                DailyLimit = 1000,
                HourlyLimit = 1000,
                BatchSize = batchSize,
                PerProviderBatchSize = 20,
                SandboxRedirectTo = sandboxRedirectTo ?? string.Empty,
                InCycleFallbackEnabled = inCycleFallback,
                MaxFallbackProvidersPerItem = maxFallbackProviders
            };

            // Defaults seguros: nenhuma pendência em nenhum provider, nada para retry.
            QueueRepo
                .Setup(r => r.GetPendingBatchByProviderAsync(It.IsAny<EmailProvider>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            QueueRepo
                .Setup(r => r.GetStaleProcessingAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            QueueRepo
                .Setup(r => r.GetFailedForRetryAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            QueueRepo
                .Setup(r => r.GetExhaustedFailedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            QueueRepo
                .Setup(r => r.CountSentSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            QueueRepo
                .Setup(r => r.CountSentByProviderSinceAsync(It.IsAny<EmailProvider>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Defaults: ninguém suprimido, nenhum customer conhecido (só a suppression valeria).
            SuppressionRepo
                .Setup(r => r.IsSuppressedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            CustomerRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);
            CustomerRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            var chainOptions = new EmailChainOptions
            {
                ProviderDailyLimits = providerDailyLimits ?? new()
            };
            var chainMonitor = new Mock<IOptionsMonitor<EmailChainOptions>>();
            chainMonitor.Setup(m => m.CurrentValue).Returns(chainOptions);

            var services = new ServiceCollection();
            foreach (var key in new[] { "brevo", "mailjet", "resend", "elasticemail", "mailersend", "sendgrid" })
            {
                services.AddKeyedSingleton<IEmailSender>(key, Sender);
            }

            Processor = new EmailQueueCycleProcessor(
                QueueRepo.Object,
                CampaignRepo.Object,
                CustomerRepo.Object,
                SuppressionRepo.Object,
                new EmailTemplateEngine(),
                new Mock<IUnitOfWork>().Object,
                PilotBreaker,
                ProviderBreaker,
                EmailTestDefaults.ProviderPolicy(disabledProviders ?? []),
                EmailTestDefaults.LinkBuilder(),
                new Mock<IAuditLogRepository>().Object,
                new Mock<IUserRepository>().Object,
                services.BuildServiceProvider(),
                Options.Create(Settings),
                chainMonitor.Object,
                Alerter,
                NullLogger<EmailQueueCycleProcessor>.Instance);
        }

        public void SetupPending(EmailProvider provider, params EmailQueueItem[] items)
        {
            QueueRepo
                .Setup(r => r.GetPendingBatchByProviderAsync(provider, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailProvider _, DateTime _, int take, CancellationToken _) => items.Take(take).ToList());
        }
    }

    private static EmailQueueItem NewItem(
        EmailProvider provider = EmailProvider.Brevo,
        Guid? campaignId = null,
        string email = "lead@example.com",
        bool withCustomerId = true)
    {
        return new EmailQueueItem(
            Guid.NewGuid(),
            "Lead",
            email,
            "Assunto",
            "<p>corpo {{unsubscribe_url}}</p>",
            DateTime.UtcNow.AddMinutes(-1),
            customerId: withCustomerId ? Guid.NewGuid() : null,
            campaignId: campaignId,
            assignedProvider: provider);
    }

    private static void SetProcessingStartedAt(EmailQueueItem item, DateTime value)
        => typeof(EmailQueueItem).GetProperty(nameof(EmailQueueItem.ProcessingStartedAt))!
            .SetValue(item, value);

    // ───── P0-3: recovery de Processing órfão ─────

    [Fact]
    public async Task StaleProcessing_WithRemainingAttempts_IsRequeued()
    {
        var h = new Harness();
        var stale = NewItem();
        stale.MarkProcessing(); // AttemptCount = 1
        SetProcessingStartedAt(stale, DateTime.UtcNow.AddMinutes(-45));
        h.QueueRepo
            .Setup(r => r.GetStaleProcessingAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([stale]);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(EmailQueueStatus.Queued, stale.Status); // resgatado, não órfão
        h.QueueRepo.Verify(r => r.UpdateAsync(stale, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StaleProcessing_AtMaxAttempts_IsDeadLetteredAndCounted()
    {
        var h = new Harness();
        var campaignId = Guid.NewGuid();
        var stale = NewItem(campaignId: campaignId);
        stale.MarkProcessing();
        stale.MarkProcessing();
        stale.MarkProcessing(); // AttemptCount = 3 = máximo
        SetProcessingStartedAt(stale, DateTime.UtcNow.AddMinutes(-45));
        h.QueueRepo
            .Setup(r => r.GetStaleProcessingAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([stale]);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        // Antes: Failed terminal invisível. Agora: DLQ visível + alerta.
        Assert.Equal(EmailQueueStatus.DeadLettered, stale.Status);
        Assert.Contains("Processing", stale.LastError);
        h.CampaignRepo.Verify(r => r.IncrementFailedAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(h.Alerter.Alerts, a => a.Key == "dlq:stale");
    }

    // ───── P1-7: providers desabilitados e reassinalação no retry ─────

    [Fact]
    public async Task ItemsOnDisabledProvider_AreReassignedToEnabledProvider()
    {
        var h = new Harness(disabledProviders: ["mailersend", "elasticemail"]);
        var orphan = NewItem(EmailProvider.MailerSend);
        h.SetupPending(EmailProvider.MailerSend, orphan);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.NotEqual(EmailProvider.MailerSend, orphan.AssignedProvider);
        Assert.NotEqual(EmailProvider.ElasticEmail, orphan.AssignedProvider);
        h.QueueRepo.Verify(r => r.UpdateAsync(orphan, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Retry_ReassignsProvider_AndDecrementsFailedCount()
    {
        var h = new Harness();
        var campaignId = Guid.NewGuid();
        var failed = NewItem(EmailProvider.Brevo, campaignId);
        failed.MarkProcessing();               // AttemptCount = 1
        failed.MarkFailed("erro 500 do brevo");
        h.QueueRepo
            .Setup(r => r.GetFailedForRetryAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([failed]);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(EmailQueueStatus.Queued, failed.Status);
        Assert.NotEqual(EmailProvider.Brevo, failed.AssignedProvider); // não insiste no que falhou
        Assert.NotNull(failed.LastError); // erro preservado para diagnóstico
        // P1-12: o Failed não era terminal — desconta para não inflar Sent+Failed
        h.CampaignRepo.Verify(r => r.DecrementFailedAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───── P1-8: breaker global abre no meio do lote → ciclo PARA ─────

    [Fact]
    public async Task PilotBreakerOpensMidBatch_CycleStopsImmediately()
    {
        var failingSender = new QueueFakeSender(_ => EmailSendResult.Fail("erro 500 catastrófico"));
        var h = new Harness(sender: failingSender);

        // Janela do pilot já com 2 falhas: a próxima abre o breaker (3/3 ≥ 30%).
        h.PilotBreaker.RecordFailure("erro anterior 1");
        h.PilotBreaker.RecordFailure("erro anterior 2");

        var campaignId = Guid.NewGuid();
        var item1 = NewItem(EmailProvider.Brevo, campaignId, "um@a.com");
        var item2 = NewItem(EmailProvider.Brevo, campaignId, "dois@a.com");
        h.SetupPending(EmailProvider.Brevo, item1, item2);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.True(h.PilotBreaker.IsOpen);
        Assert.Single(failingSender.Sent);                       // só o 1º item foi tentado
        Assert.Equal(EmailQueueStatus.Failed, item1.Status);
        Assert.Equal(EmailQueueStatus.Queued, item2.Status);     // 2º intocado — ciclo parou
    }

    // ───── limites por provider (P1-10) ─────

    [Fact]
    public async Task ProviderDailyLimitReached_SkipsProvider()
    {
        var h = new Harness(providerDailyLimits: new() { ["brevo"] = 10 });
        h.QueueRepo
            .Setup(r => r.CountSentByProviderSinceAsync(EmailProvider.Brevo, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // teto do dia já atingido
        var item = NewItem(EmailProvider.Brevo);
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Empty(h.Sender.Sent);
        Assert.Equal(EmailQueueStatus.Queued, item.Status); // aguarda o reset diário
    }

    [Fact]
    public async Task GlobalBatchSize_LimitsTotalAcrossProviders()
    {
        var h = new Harness(batchSize: 10);
        var brevoItems = Enumerable.Range(1, 6)
            .Select(i => NewItem(EmailProvider.Brevo, email: $"brevo-{i}@example.com"))
            .ToArray();
        var mailjetItems = Enumerable.Range(1, 6)
            .Select(i => NewItem(EmailProvider.Mailjet, email: $"mailjet-{i}@example.com"))
            .ToArray();
        h.SetupPending(EmailProvider.Brevo, brevoItems);
        h.SetupPending(EmailProvider.Mailjet, mailjetItems);

        var processed = await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(10, processed);
        Assert.Equal(10, h.Sender.Sent.Count);
        Assert.All(brevoItems, item => Assert.Equal(EmailQueueStatus.Sent, item.Status));
        Assert.Equal(4, mailjetItems.Count(item => item.Status == EmailQueueStatus.Sent));
        Assert.Equal(2, mailjetItems.Count(item => item.Status == EmailQueueStatus.Queued));
        h.QueueRepo.Verify(
            r => r.GetPendingBatchByProviderAsync(
                EmailProvider.Mailjet,
                It.IsAny<DateTime>(),
                4,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ───── sandbox mode ─────

    [Fact]
    public async Task SandboxMode_RedirectsRecipient_AndTagsSubject()
    {
        var h = new Harness(sandboxRedirectTo: "sandbox@test.local");
        var item = NewItem(EmailProvider.Brevo, email: "lead-real@cliente.com");
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        var sent = Assert.Single(h.Sender.Sent);
        Assert.Equal("sandbox@test.local", sent.RecipientEmail);
        Assert.Contains("[SANDBOX p/ lead-real@cliente.com]", sent.Subject);
        Assert.Equal(EmailQueueStatus.Sent, item.Status);
    }

    // ───── fallback in-cycle entre providers (auditoria §11) ─────

    [Fact]
    public async Task SendFails_ProviderLevel_FallsBackToNextProviderSameCycle()
    {
        // 1ª chamada (titular brevo) falha; 2ª (fallback) sucede — mesmo fake registrado em todas as keys.
        var calls = 0;
        var sender = new QueueFakeSender(_ => ++calls == 1
            ? EmailSendResult.Fail("erro 500 do provider")
            : EmailSendResult.Ok("fb-msg"));
        var h = new Harness(sender: sender, inCycleFallback: true);
        var campaignId = Guid.NewGuid();
        var item = NewItem(EmailProvider.Brevo, campaignId);
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(EmailQueueStatus.Sent, item.Status);
        Assert.NotEqual(EmailProvider.Brevo, item.AssignedProvider); // reatribuído ao provider que salvou
        Assert.Equal(2, sender.Sent.Count);                          // titular + 1 fallback, MESMO ciclo
        h.CampaignRepo.Verify(r => r.IncrementSentAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        h.CampaignRepo.Verify(r => r.IncrementFailedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendFails_RecipientError_DoesNotFallback()
    {
        var sender = new QueueFakeSender(_ => EmailSendResult.Fail("bounce: mailbox not found"));
        var h = new Harness(sender: sender, inCycleFallback: true);
        var item = NewItem(EmailProvider.Brevo);
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Single(sender.Sent); // endereço ruim: NÃO queima os outros providers
        Assert.Equal(EmailQueueStatus.Failed, item.Status); // tentativa 1 de 3 — segue para retry
    }

    [Fact]
    public async Task SendFails_AllFallbacksFail_MarksFailedWithErrorChain()
    {
        var sender = new QueueFakeSender(_ => EmailSendResult.Fail("erro 500"));
        var h = new Harness(sender: sender, inCycleFallback: true, maxFallbackProviders: 2);
        var item = NewItem(EmailProvider.Brevo);
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(3, sender.Sent.Count); // titular + 2 fallbacks
        Assert.Equal(EmailQueueStatus.Failed, item.Status);
        Assert.Contains("|", item.LastError); // cadeia de erros por provider preservada
    }

    // ───── DLQ: tentativas esgotadas viram DeadLettered + alerta ─────

    [Fact]
    public async Task ThirdAttemptFails_GoesToDeadLetter_AndAlerts()
    {
        var sender = new QueueFakeSender(_ => EmailSendResult.Fail("erro 500"));
        var h = new Harness(sender: sender);
        var campaignId = Guid.NewGuid();
        var item = NewItem(EmailProvider.Brevo, campaignId);
        item.MarkProcessing();
        item.MarkProcessing();               // AttemptCount = 2
        item.Requeue(DateTime.UtcNow.AddMinutes(-1));
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(EmailQueueStatus.DeadLettered, item.Status); // 3ª falha = fim da linha visível
        h.CampaignRepo.Verify(r => r.IncrementFailedAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(h.Alerter.Alerts, a => a.Key.StartsWith("dlq:") && a.Message.Contains(item.RecipientEmail));
    }

    [Fact]
    public async Task ExhaustedFailedLegacy_IsSweptToDeadLetter()
    {
        var h = new Harness();
        var legacy = NewItem(EmailProvider.Brevo);
        legacy.MarkProcessing();
        legacy.MarkProcessing();
        legacy.MarkProcessing();             // AttemptCount = 3
        legacy.MarkFailed("erro antigo");
        h.QueueRepo
            .Setup(r => r.GetExhaustedFailedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([legacy]);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(EmailQueueStatus.DeadLettered, legacy.Status);
        Assert.Contains(h.Alerter.Alerts, a => a.Key == "dlq:sweep");
    }

    // ───── caminho feliz continua funcionando ─────

    [Fact]
    public async Task HappyPath_SendsAndMarksSent_WithRealUnsubscribeUrl()
    {
        var h = new Harness();
        var campaignId = Guid.NewGuid();
        var item = NewItem(EmailProvider.Brevo, campaignId);
        h.SetupPending(EmailProvider.Brevo, item);

        var processed = await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(EmailQueueStatus.Sent, item.Status);
        var sent = Assert.Single(h.Sender.Sent);
        // P0-1: variável renderizada com o host público + token, não o domínio morto
        Assert.Contains($"{EmailTestDefaults.PublicBaseUrl}/unsubscribe?token=", sent.HtmlBody);
        Assert.DoesNotContain("diaxcrm.com.br", sent.HtmlBody);
        h.CampaignRepo.Verify(r => r.IncrementSentAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───── HARDENING: recheck de suppression/opt-out no DESPACHO ─────
    // Antes só o enqueue checava — bounce/unsubscribe entre o agendamento e o
    // despacho deixava o item enfileirado sair mesmo assim.

    [Fact]
    public async Task SuppressedAtDispatch_IsCancelled_NotSent_NoRetryNoDlq()
    {
        var h = new Harness();
        var campaignId = Guid.NewGuid();
        var item = NewItem(EmailProvider.Brevo, campaignId, "bounced@cliente.com");
        h.SetupPending(EmailProvider.Brevo, item);
        h.SuppressionRepo
            .Setup(r => r.IsSuppressedAsync(item.UserId, "bounced@cliente.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // entrou na suppression DEPOIS do enqueue

        var processed = await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Empty(h.Sender.Sent);                              // nada sai para o provider
        Assert.Equal(EmailQueueStatus.Cancelled, item.Status);    // terminal, não Failed/DLQ
        Assert.Equal(0, item.AttemptCount);                       // não conta como tentativa de retry
        Assert.Contains("suppressed at dispatch", item.LastError);
        Assert.Equal(1, processed);                               // consumiu o slot do ciclo
        // Não é erro de provider: sem alerta de DLQ e sem abrir breaker.
        Assert.DoesNotContain(h.Alerter.Alerts, a => a.Key.StartsWith("dlq:"));
        Assert.False(h.PilotBreaker.IsOpen);
        // Contabilidade da campanha fecha (CheckCompletion soma Sent+Failed).
        h.CampaignRepo.Verify(r => r.IncrementFailedAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        h.QueueRepo.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CustomerOptOutAtDispatch_IsCancelled_NotSent()
    {
        var h = new Harness();
        var item = NewItem(EmailProvider.Brevo);
        h.SetupPending(EmailProvider.Brevo, item);

        var optedOut = new Customer("Lead", item.RecipientEmail);
        optedOut.OptOutEmail(); // opt-out DEPOIS do enqueue
        h.CustomerRepo
            .Setup(r => r.GetByIdAsync(item.CustomerId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optedOut);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Empty(h.Sender.Sent);
        Assert.Equal(EmailQueueStatus.Cancelled, item.Status);
        Assert.Equal(0, item.AttemptCount);
        Assert.Contains("opt-out", item.LastError);
        Assert.DoesNotContain(h.Alerter.Alerts, a => a.Key.StartsWith("dlq:"));
    }

    [Fact]
    public async Task OptOutWithoutCustomerId_IsFoundByEmail_AndCancelled()
    {
        var h = new Harness();
        var item = NewItem(EmailProvider.Brevo, email: "avulso@cliente.com", withCustomerId: false);
        h.SetupPending(EmailProvider.Brevo, item);

        var optedOut = new Customer("Avulso", "avulso@cliente.com");
        optedOut.OptOutEmail();
        h.CustomerRepo
            .Setup(r => r.GetByEmailAsync("avulso@cliente.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(optedOut);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        Assert.Empty(h.Sender.Sent);
        Assert.Equal(EmailQueueStatus.Cancelled, item.Status);
    }

    [Fact]
    public async Task NotSuppressedAndNoOptOut_SendsNormally()
    {
        var h = new Harness();
        var item = NewItem(EmailProvider.Brevo);
        h.SetupPending(EmailProvider.Brevo, item);

        await h.Processor.ProcessOnceAsync(CancellationToken.None);

        // O recheck foi consultado com o dono e o email do item — e o envio seguiu.
        h.SuppressionRepo.Verify(
            r => r.IsSuppressedAsync(item.UserId, item.RecipientEmail, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Single(h.Sender.Sent);
        Assert.Equal(EmailQueueStatus.Sent, item.Status);
    }
}
