namespace Diax.Application.Customers.Services;

/// <summary>
/// Resultado da checagem de entregabilidade do domínio de e-mail (EXTR-01).
/// </summary>
public enum MxCheckResult
{
    /// <summary>
    /// MX resolvido — ou, na ausência de MX, registro A do domínio (fallback RFC 5321,
    /// mesmo comportamento de `resolve_mx` em docs/email-marketing/mx_check.py).
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Domínio comprovadamente sem entrega: NXDOMAIN, Null MX (RFC 7505) sem A, ou
    /// resposta vazia sem A. Este é o ÚNICO estado que rejeita o lead.
    /// </summary>
    NoMx = 1,

    /// <summary>
    /// Timeout / falha de infraestrutura DNS. Decisão D-02 do 07-CONTEXT.md:
    /// falha de infraestrutura NÃO é prova de lead ruim — o lead PASSA, marcado como
    /// "MX não verificado". Nunca tratar como NoMx.
    /// </summary>
    Unverified = 2
}

/// <summary>
/// Seam de I/O de rede para checagem de MX. Existe para que os filtros de qualidade do
/// ExtractorIntegrationService sejam testáveis offline (Mock&lt;IMxLookupService&gt;), no mesmo
/// padrão de IExtractorService / ICustomerRepository.
/// </summary>
public interface IMxLookupService
{
    /// <summary>
    /// Checa o domínio (parte depois do '@', sem '@', case-insensitive).
    /// NUNCA lança: qualquer falha de rede vira <see cref="MxCheckResult.Unverified"/>.
    /// </summary>
    Task<MxCheckResult> CheckAsync(string domain, CancellationToken cancellationToken = default);
}
