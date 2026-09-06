using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>
/// Cache persistente do resultado da checagem de MX por DOMÍNIO (decisão D-03), espelhando o
/// `previews/mx-cache.json` (30 dias) da ponte Python.
///
/// Por que tabela e não IMemoryCache: o App Pool do IIS em shared hosting recicla com frequência
/// (idle timeout, deploy FTP, memory pressure) — um cache em processo de 30 dias não sobrevive.
/// Volume: domínios ÚNICOS vistos pelo Extrator (milhares), não linhas por lead.
///
/// ResultCode é `int` (não o enum MxCheckResult) porque MxCheckResult vive em Diax.Application
/// e Diax.Domain NÃO referencia Application. A conversão é feita na camada Application.
/// Valores: 0 = Valid, 1 = NoMx, 2 = Unverified (espelha MxCheckResult).
/// </summary>
public class MxCacheEntry : AuditableEntity
{
    /// <summary>Domínio normalizado (lowercase, sem espaço, sem '@').</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Espelha MxCheckResult: 0 = Valid, 1 = NoMx, 2 = Unverified.</summary>
    public int ResultCode { get; private set; }

    /// <summary>Momento (UTC) da última resolução real por DNS.</summary>
    public DateTime CheckedAt { get; private set; }

    /// <summary>Construtor para EF Core.</summary>
    protected MxCacheEntry() { }

    public MxCacheEntry(string domain, int resultCode)
    {
        Domain = Normalize(domain);
        ResultCode = resultCode;
        CheckedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Construtor usado por seeds/testes para forjar a idade da entrada (TTL determinístico).
    /// Produção sempre usa o construtor de 2 args (CheckedAt = DateTime.UtcNow).
    /// </summary>
    public MxCacheEntry(string domain, int resultCode, DateTime checkedAtUtc)
    {
        Domain = Normalize(domain);
        ResultCode = resultCode;
        CheckedAt = checkedAtUtc;
    }

    /// <summary>Regrava o resultado e reinicia o TTL.</summary>
    public void Refresh(int resultCode)
    {
        ResultCode = resultCode;
        CheckedAt = DateTime.UtcNow;
    }

    /// <summary>True se a entrada ainda está dentro da janela de validade informada.</summary>
    public bool IsFresh(DateTime nowUtc, TimeSpan validFor) => nowUtc - CheckedAt < validFor;

    /// <summary>Normalização canônica do domínio — usar SEMPRE antes de consultar o cache.</summary>
    public static string Normalize(string? domain)
        => (domain ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();
}
