using Diax.Domain.EmailMarketing;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class EmailSendLogRepository : IEmailSendLogRepository
{
    private readonly DiaxDbContext _db;

    public EmailSendLogRepository(DiaxDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<EmailSendLog?> FindRecentByIdempotencyKeyAsync(
        string idempotencyKey,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - window;
        return await _db.EmailSendLogs
            .Where(x => x.IdempotencyKey == idempotencyKey && x.CreatedAt >= cutoff)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<EmailSendLog> CreateInFlightAsync(
        string requestId,
        string? idempotencyKey,
        string toHash,
        string subjectHash,
        string? bodyHash,
        string fromDomain,
        CancellationToken ct = default)
    {
        var log = EmailSendLog.CreateInFlight(
            requestId, idempotencyKey, toHash, subjectHash, bodyHash, fromDomain);

        await _db.EmailSendLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
        return log;
    }

    /// <inheritdoc/>
    public async Task RecordAttemptAsync(
        Guid logId,
        string provider,
        int attemptNo,
        bool success,
        string? providerMessageId,
        string? error,
        int latencyMs,
        bool allowUnaligned,
        CancellationToken ct = default)
    {
        var log = await _db.EmailSendLogs.FindAsync([logId], ct);
        if (log is null) return;

        log.RecordAttempt(provider, attemptNo, success, providerMessageId, error, latencyMs, allowUnaligned);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task MarkSentAsync(
        Guid logId,
        string provider,
        string? providerMessageId,
        bool allowUnaligned,
        CancellationToken ct = default)
    {
        var log = await _db.EmailSendLogs.FindAsync([logId], ct);
        if (log is null) return;

        log.MarkSent(provider, providerMessageId, allowUnaligned);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task MarkFailedAsync(Guid logId, CancellationToken ct = default)
    {
        var log = await _db.EmailSendLogs.FindAsync([logId], ct);
        if (log is null) return;

        log.MarkFailed();
        await _db.SaveChangesAsync(ct);
    }
}
