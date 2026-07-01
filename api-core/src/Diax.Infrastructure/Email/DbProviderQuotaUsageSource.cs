using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Diax.Infrastructure.Email;

/// <summary>
/// Conta os envios reais registrados no banco para um provider desde um instante —
/// soma o dispatch unificado (email_send_log, status Sent) com a fila de campanhas
/// (email_queue_items, status Sent). É a fonte de hidratação do ProviderQuotaGuard
/// após restart/recycle do processo.
/// </summary>
public sealed class DbProviderQuotaUsageSource : IProviderQuotaUsageSource
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbProviderQuotaUsageSource(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<int> GetUsedSinceAsync(string providerKey, DateTime sinceUtc, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();

        var sendLogCount = await db.EmailSendLogs
            .CountAsync(x => x.Provider == providerKey && x.Status == "Sent" && x.CreatedAt >= sinceUtc, ct);

        // Providers SMTP (Tier 2) não têm enum na fila de campanhas — só o send log conta.
        var queueCount = 0;
        var policy = scope.ServiceProvider.GetRequiredService<IEmailProviderPolicy>();
        if (policy.TryParse(providerKey, out var provider))
        {
            queueCount = await db.EmailQueueItems
                .CountAsync(x => x.AssignedProvider == provider
                              && x.Status == EmailQueueStatus.Sent
                              && x.SentAt != null
                              && x.SentAt >= sinceUtc, ct);
        }

        return sendLogCount + queueCount;
    }
}
