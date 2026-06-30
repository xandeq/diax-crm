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
    private readonly IEmailSendLogRepository _logRepo;
    private readonly ILogger<EmailFallbackOrchestrator> _logger;

    public EmailFallbackOrchestrator(
        IServiceProvider sp,
        IOptionsMonitor<EmailChainOptions> chain,
        IProviderCircuitBreaker breaker,
        IEmailSendLogRepository logRepo,
        ILogger<EmailFallbackOrchestrator> logger)
    {
        _sp = sp;
        _chain = chain;
        _breaker = breaker;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> DispatchAsync(EmailDispatchRequest request, CancellationToken ct = default)
    {
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
                    _logger.LogInformation("Idempotency — chave {Key} em processamento", request.IdempotencyKey);
                    return new EmailDispatchResult(false, EmailDispatchStatus.InProgress, null, null, false, []);
                }
            }
        }

        // 2. Validate sender domain
        var options = _chain.CurrentValue;
        if (!options.SenderDomains.TryGetValue(fromDomain, out var domainConfig))
        {
            _logger.LogWarning("From domain {Domain} não configurado em EmailChain:SenderDomains", fromDomain);
            return new EmailDispatchResult(false, EmailDispatchStatus.Rejected, null, null, false, []);
        }

        // 3. Create InFlight log
        var log = await _logRepo.CreateInFlightAsync(
            request.RequestId, request.IdempotencyKey,
            toHash, subjectHash, bodyHash, fromDomain, ct);

        // 4. Build ordered provider list
        var providers = new List<string>(domainConfig.Tier1Providers);
        var tier2Set = new HashSet<string>(domainConfig.Tier2Providers, StringComparer.OrdinalIgnoreCase);

        if (request.AllowUnaligned)
            providers.AddRange(domainConfig.Tier2Providers);

        // 5. Hard timeout — wrap caller's token
        using var hardCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        hardCts.CancelAfter(options.HardTimeout);
        var linkedCt = hardCts.Token;

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

            var sender = _sp.GetKeyedService<IEmailSender>(providerKey);
            if (sender is null)
            {
                _logger.LogWarning("Provider {Provider} não registrado no DI", providerKey);
                continue;
            }

            var attemptNo = attempts.Count + 1;
            var sw = Stopwatch.StartNew();
            EmailSendResult result;

            try
            {
                result = await sender.SendAsync(request.Message, linkedCt);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                var timeoutDetail = new EmailAttemptDetail(providerKey, attemptNo, false, null, "Hard timeout (60s)", sw.ElapsedMilliseconds);
                attempts.Add(timeoutDetail);
                await _logRepo.RecordAttemptAsync(log.Id, providerKey, attemptNo, false, null, "Hard timeout", (int)sw.ElapsedMilliseconds, isTier2, ct);
                _breaker.RecordFailure(providerKey, "timeout");
                break; // Hard timeout — abort chain
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
