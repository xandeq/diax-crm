using Diax.Application.Customers.Services;
using DnsClient;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Dns;

/// <summary>
/// Implementação real de IMxLookupService com DnsClient.NET 1.8.0 (decisão D-01).
/// Toda a decisão de negócio vive em MxResponseInterpreter (puro/testado); esta classe é só
/// o glue tipado com a lib.
/// </summary>
public class DnsClientMxLookupService : IMxLookupService
{
    private readonly ILookupClient _lookup;
    private readonly ILogger<DnsClientMxLookupService> _logger;

    public DnsClientMxLookupService(ILookupClient lookup, ILogger<DnsClientMxLookupService> logger)
    {
        _lookup = lookup;
        _logger = logger;
    }

    public async Task<MxCheckResult> CheckAsync(string domain, CancellationToken cancellationToken = default)
    {
        var d = (domain ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();
        if (string.IsNullOrEmpty(d))
            return MxCheckResult.NoMx;

        try
        {
            var mxResponse = await _lookup.QueryAsync(d, QueryType.MX, cancellationToken: cancellationToken);

            var exchanges = mxResponse.Answers.MxRecords()
                .Select(r => r.Exchange.Value)
                .ToList();

            var step = MxResponseInterpreter.InterpretMxResponse(
                exchanges,
                mxResponse.HasError,
                (int)mxResponse.Header.ResponseCode);

            if (step == MxResolutionStep.Valid) return MxCheckResult.Valid;
            if (step == MxResolutionStep.NoMx) return MxCheckResult.NoMx;
            if (step == MxResolutionStep.Unverified)
            {
                // SERVFAIL/NotImplemented/Refused: falha do servidor DNS (D-02) — lead passa.
                _logger.LogDebug("MX lookup de {Domain} voltou RCODE {Rcode} (falha do servidor DNS) — marcando como não verificado.",
                    d, (int)mxResponse.Header.ResponseCode);
                return MxCheckResult.Unverified;
            }

            // Sem MX / Null MX: RFC 5321 permite usar o registro A do próprio domínio.
            var aResponse = await _lookup.QueryAsync(d, QueryType.A, cancellationToken: cancellationToken);
            return MxResponseInterpreter.InterpretAFallback(
                aResponse.Answers.ARecords().Any(),
                aResponse.HasError,
                (int)aResponse.Header.ResponseCode);
        }
        catch (DnsResponseException ex) when (ex.Code == DnsResponseCode.ConnectionTimeout)
        {
            // D-02: falha de INFRAESTRUTURA não rejeita o lead.
            _logger.LogDebug("MX lookup de {Domain} expirou (ConnectionTimeout) — marcando como não verificado.", d);
            return MxCheckResult.Unverified;
        }
        catch (Exception ex)
        {
            // Qualquer outra falha (socket, DNS mal configurado no host, etc.) também é
            // infraestrutura — degrada para "não verificado", nunca para NoMx.
            _logger.LogWarning(ex, "MX lookup de {Domain} falhou de forma inesperada — marcando como não verificado.", d);
            return MxCheckResult.Unverified;
        }
    }
}
