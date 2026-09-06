using Diax.Application.Customers.Services;

namespace Diax.Infrastructure.Dns;

/// <summary>Próximo passo depois de olhar a resposta MX.</summary>
public enum MxResolutionStep
{
    /// <summary>Há MX real — domínio entrega e-mail.</summary>
    Valid = 0,

    /// <summary>NXDOMAIN: o domínio não existe. Não vale nem consultar o registro A.</summary>
    NoMx = 1,

    /// <summary>Sem MX (ou Null MX): RFC 5321 permite cair no registro A do domínio.</summary>
    TryARecordFallback = 2,

    /// <summary>
    /// Falha do SERVIDOR DNS (SERVFAIL/NotImplemented/Refused): a resposta não afirma nada sobre
    /// o domínio. D-02 — infraestrutura quebrada não rejeita lead. Não vale nem tentar o A: se a
    /// query A falhar pela mesma causa, o fallback devolveria NoMx e rejeitaria um lead bom.
    /// </summary>
    Unverified = 3
}

/// <summary>
/// Decisão pura sobre uma resposta DNS de MX, isolada do DnsClient.NET para ser testável sem
/// rede e sem construir tipos da lib. Porta de `parse_nslookup_mx` + `resolve_mx`
/// (docs/email-marketing/mx_check.py linhas 31-49).
/// </summary>
public static class MxResponseInterpreter
{
    /// <summary>RCODE 3 = NXDOMAIN (DnsResponseCode.NotExistentDomain).</summary>
    public const int NxDomainResponseCode = 3;

    /// <summary>Null MX (RFC 7505): único registro MX com Exchange igual à raiz.</summary>
    public const string NullMxExchange = ".";

    /// <summary>
    /// RCODEs de FALHA DO SERVIDOR (não são afirmações sobre o domínio):
    /// 2 = SERVFAIL, 4 = NotImplemented, 5 = Refused. Mesma classe de ConnectionTimeout (D-02).
    /// NXDOMAIN (3) NÃO entra aqui — é uma afirmação positiva de que o domínio não existe.
    /// </summary>
    public static bool IsTransientServerFailure(int responseCode)
        => responseCode is 2 or 4 or 5;

    public static MxResolutionStep InterpretMxResponse(
        IReadOnlyList<string> mxExchanges,
        bool hasError,
        int responseCode)
    {
        var real = mxExchanges
            .Where(e => !string.IsNullOrWhiteSpace(e) && e.Trim() != NullMxExchange)
            .ToList();

        if (real.Count > 0)
            return MxResolutionStep.Valid;

        if (hasError && responseCode == NxDomainResponseCode)
            return MxResolutionStep.NoMx;

        // D-02: erro do servidor DNS nunca rejeita lead — e nem adianta tentar o A.
        if (hasError && IsTransientServerFailure(responseCode))
            return MxResolutionStep.Unverified;

        return MxResolutionStep.TryARecordFallback;
    }

    /// <summary>
    /// Decide o resultado final depois da query de registro A (fallback RFC 5321).
    /// `hasError`/`responseCode` têm default para preservar as chamadas simples nos testes.
    /// </summary>
    public static MxCheckResult InterpretAFallback(
        bool hasARecords,
        bool hasError = false,
        int responseCode = 0)
    {
        if (hasARecords)
            return MxCheckResult.Valid;

        // D-02 de novo: se a própria query A falhou por erro de servidor, não afirmar "sem MX".
        if (hasError && IsTransientServerFailure(responseCode))
            return MxCheckResult.Unverified;

        return MxCheckResult.NoMx;
    }
}
