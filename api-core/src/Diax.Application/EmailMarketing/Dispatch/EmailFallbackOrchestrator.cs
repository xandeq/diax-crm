using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Diax.Domain.EmailMarketing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diax.Application.EmailMarketing.Dispatch;

public class EmailFallbackOrchestrator : IEmailDispatchService
{
    private readonly IServiceProvider _sp;
    private readonly IOptionsMonitor<EmailChainOptions> _chain;
    private readonly IProviderCircuitBreaker _breaker;
    private readonly IProviderQuotaGuard _quota;
    private readonly IEmailSendLogRepository _logRepo;
    private readonly ILogger<EmailFallbackOrchestrator> _logger;

    public EmailFallbackOrchestrator(
        IServiceProvider sp,
        IOptionsMonitor<EmailChainOptions> chain,
        IProviderCircuitBreaker breaker,
        IProviderQuotaGuard quota,
        IEmailSendLogRepository logRepo,
        ILogger<EmailFallbackOrchestrator> logger)
    {
        _sp = sp;
        _chain = chain;
        _breaker = breaker;
        _quota = quota;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> DispatchAsync(EmailDispatchRequest request, CancellationToken ct = default)
    {
        var options = _chain.CurrentValue;
        var fromDomain = ExtractDomain(request.Message.From.Address);
        var toHash = HashAddresses(request.Message.To);
        var subjectHash = Hash(request.Message.Subject);
        var bodyHash = Hash(request.Message.Html);

        // 1. Idempotency check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _logRepo.FindRecentByIdempotencyKeyAsync(
                request.IdempotencyKey, TimeSpan.FromHours(24), ct);

            if (existing is not null)
            {
                if (existing.Status == "Sent")
                {
                    _logger.LogInformation("Idempotency replay — chave {Key} já enviada", request.IdempotencyKey);
                    return new EmailDispatchResult(true, EmailDispatchStatus.Duplicate,
                        existing.ProviderMessageId, existing.Provider, existing.AllowUnaligned, []);
                }
                if (existing.Status == "InFlight")
                {
                    var age = DateTime.UtcNow - existing.CreatedAt;
                    if (age < options.InFlightStaleAfter)
                    {
                        _logger.LogInformation("Idempotency — chave {Key} em processamento", request.IdempotencyKey);
                        return new EmailDispatchResult(false, EmailDispatchStatus.InProgress, null, null, false, []);
                    }

                    // InFlight órfão (crash no meio do dispatch): sem esta expiração a chave
                    // ficaria bloqueada por 24h respondendo 409. Marca Uncertain e libera.
                    _logger.LogWarning(
                        "Idempotency — InFlight órfão ({Age:F0}min) para chave {Key}; marcando Uncertain e reprocessando",
                        age.TotalMinutes, request.IdempotencyKey);
                    await _logRepo.MarkUncertainAsync(existing.Id, "InFlight órfão expirado — possível crash durante dispatch", ct);
                }
                // Status Failed/Uncertain: caller re-tentou explicitamente — prossegue.
            }
        }

        // 2. Validate sender domain
        if (!options.SenderDomains.TryGetValue(fromDomain, out var domainConfig))
        {
            _logger.LogWarning("From domain {Domain} não configurado em EmailChain:SenderDomains", fromDomain);
            return new EmailDispatchResult(false, EmailDispatchStatus.Rejected, null, null, false, []);
        }

        // 3. Create InFlight log — o índice único filtrado em idempotency_key garante que
        //    apenas UMA chamada concorrente detém a chave; a perdedora recebe InProgress.
        var log = await _logRepo.TryCreateInFlightAsync(
            request.RequestId, request.IdempotencyKey,
            toHash, subjectHash, bodyHash, fromDomain, ct);

        if (log is null)
        {
            _logger.LogInformation(
                "Idempotency — corrida perdida para chave {Key} (outra chamada detém o InFlight)",
                request.IdempotencyKey);
            return new EmailDispatchResult(false, EmailDispatchStatus.InProgress, null, null, false, []);
        }

        // 4. Build ordered provider list (ProviderHint vai para a frente do seu tier)
        var tier2Set = new HashSet<string>(domainConfig.Tier2Providers, StringComparer.OrdinalIgnoreCase);
        var providers = BuildProviderOrder(domainConfig, request.ProviderHint, request.AllowUnaligned);

        // 5. Hard timeout — orçamento TOTAL da cadeia; cada provider tem o próprio orçamento.
        using var hardCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        hardCts.CancelAfter(options.HardTimeout);
        var chainCt = hardCts.Token;

        var attempts = new List<EmailAttemptDetail>();

        foreach (var providerKey in providers)
        {
            // Tier 2 gate: never fall through without explicit consent
            var isTier2 = tier2Set.Contains(providerKey);
            if (isTier2 && !request.AllowUnaligned)
                break;

            if (_breaker.IsOpen(providerKey))
            {
                _logger.LogDebug("Circuit breaker aberto para {Provider} — pulando", providerKey);
                continue;
            }

            if (!await _quota.TryConsumeAsync(providerKey, ct))
            {
                // Quota esgotada — não consome crédito, pula para o próximo
                continue;
            }

            var sender = _sp.GetKeyedService<IEmailSender>(providerKey);
            if (sender is null)
            {
                _logger.LogWarning("Provider {Provider} não registrado no DI", providerKey);
                continue;
            }

            var attemptNo = attempts.Count + 1;
            var sw = Stopwatch.StartNew();
            EmailSendResult result;

            // Timeout POR PROVIDER dentro do orçamento da cadeia: um provider pendurado
            // não pode consumir os 60s inteiros e impedir os demais de tentar.
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(chainCt);
            attemptCts.CancelAfter(options.PerProviderTimeout);

            try
            {
                result = await sender.SendAsync(request.Message, attemptCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                var chainExhausted = chainCt.IsCancellationRequested;
                var timeoutError = chainExhausted
                    ? $"Hard timeout da cadeia ({options.HardTimeout.TotalSeconds:F0}s)"
                    : $"Timeout do provider ({options.PerProviderTimeout.TotalSeconds:F0}s)";

                attempts.Add(new EmailAttemptDetail(providerKey, attemptNo, false, null, timeoutError, sw.ElapsedMilliseconds));
                await _logRepo.RecordAttemptAsync(log.Id, providerKey, attemptNo, false, null, timeoutError, (int)sw.ElapsedMilliseconds, isTier2, ct);
                _breaker.RecordFailure(providerKey, "timeout");

                if (chainExhausted)
                {
                    // Orçamento total esgotado com um envio em voo: o provider pode ter
                    // aceitado a mensagem. Resultado é AMBÍGUO — nunca marcar Failed aqui,
                    // ou um retry do caller com chave nova duplicaria o email.
                    await _logRepo.MarkUncertainAsync(log.Id, timeoutError, ct);
                    return new EmailDispatchResult(false, EmailDispatchStatus.Uncertain, null, null, false, attempts);
                }

                continue; // timeout individual — próximo provider
            }

            sw.Stop();
            var detail = new EmailAttemptDetail(providerKey, attemptNo, result.Success, result.ProviderMessageId, result.ErrorMessage, sw.ElapsedMilliseconds);
            attempts.Add(detail);

            await _logRepo.RecordAttemptAsync(log.Id, providerKey, attemptNo, result.Success,
                result.ProviderMessageId, result.ErrorMessage, (int)sw.ElapsedMilliseconds, isTier2, ct);

            if (result.Success)
            {
                _breaker.RecordSuccess(providerKey);
                await _logRepo.MarkSentAsync(log.Id, providerKey, result.ProviderMessageId, isTier2, ct);

                if (isTier2)
                    _logger.LogInformation("Email enviado via Tier 2 ({Provider}) com allowUnaligned=true", providerKey);

                return new EmailDispatchResult(true, EmailDispatchStatus.Sent,
                    result.ProviderMessageId, providerKey, isTier2, attempts);
            }

            _breaker.RecordFailure(providerKey, result.ErrorMessage);
            _logger.LogWarning("Provider {Provider} falhou (tentativa {N}): {Error}", providerKey, attemptNo, result.ErrorMessage);
        }

        // 6. All providers exhausted
        await _logRepo.MarkFailedAsync(log.Id, ct);
        return new EmailDispatchResult(false, EmailDispatchStatus.AllFailed, null, null, false, attempts);
    }

    /// <summary>
    /// Tier1 na ordem configurada (+ Tier2 quando permitido). ProviderHint move o provider
    /// indicado para a frente do SEU tier — nunca promove Tier2 sem allowUnaligned.
    /// </summary>
    private static List<string> BuildProviderOrder(
        SenderDomainConfig domainConfig,
        string? providerHint,
        bool allowUnaligned)
    {
        var tier1 = new List<string>(domainConfig.Tier1Providers);
        var tier2 = allowUnaligned ? new List<string>(domainConfig.Tier2Providers) : [];

        if (!string.IsNullOrWhiteSpace(providerHint))
        {
            var hint = providerHint.Trim();
            MoveToFront(tier1, hint);
            MoveToFront(tier2, hint);
        }

        tier1.AddRange(tier2);
        return tier1;
    }

    private static void MoveToFront(List<string> list, string key)
    {
        var index = list.FindIndex(p => p.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (index > 0)
        {
            var value = list[index];
            list.RemoveAt(index);
            list.Insert(0, value);
        }
    }

    private static string ExtractDomain(string email)
    {
        var at = email.IndexOf('@');
        return at >= 0 ? email[(at + 1)..].ToLowerInvariant() : email.ToLowerInvariant();
    }

    private static string Hash(string? input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashAddresses(IReadOnlyList<EmailAddress> addresses)
    {
        var normalized = addresses.Select(a => a.Address.ToLowerInvariant()).Order();
        return Hash(string.Join(",", normalized));
    }
}
