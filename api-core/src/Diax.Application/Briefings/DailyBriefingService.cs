using Diax.Application.Briefings.Dtos;
using Diax.Domain.Briefings;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Briefings;

public class DailyBriefingService(
    IDailyBriefingRepository repo,
    IUnitOfWork unitOfWork,
    ILogger<DailyBriefingService> logger) : IDailyBriefingService
{
    private static readonly TimeZoneInfo BrtTz = ResolveBrtTimeZone();

    /// <summary>Hoje no calendário America/São_Paulo (independe do fuso do servidor).</summary>
    public static DateOnly BrtToday()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrtTz));

    public async Task<Result<Guid>> UpsertAsync(Guid userId, IngestDailyBriefingRequest request, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Guid>(new Error("DailyBriefing.Validation", "userId inválido."));
        if (string.IsNullOrWhiteSpace(request.Source))
            return Result.Failure<Guid>(new Error("DailyBriefing.Validation", "source é obrigatório."));
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<Guid>(new Error("DailyBriefing.Validation", "title é obrigatório."));

        var today = request.Date ?? BrtToday();
        var source = request.Source.Trim().ToLowerInvariant();

        // Rotação: remove briefings de dias diferentes de hoje (só o dia corrente fica visível).
        var stale = (await repo.GetOtherDaysAsync(userId, today, ct)).ToList();
        if (stale.Count > 0)
        {
            repo.RemoveRange(stale);
            logger.LogInformation("DailyBriefing purge: {Count} briefings de dias anteriores removidos (user={UserId})", stale.Count, userId);
        }

        var existing = await repo.GetByUserDateSourceAsync(userId, today, source, ct);
        if (existing is null)
        {
            existing = new DailyBriefing(userId, today, source, request.Title, request.Content ?? string.Empty, request.Format);
            await repo.AddAsync(existing, ct);
        }
        else
        {
            existing.SetContent(request.Title, request.Content ?? string.Empty, request.Format);
            await repo.UpdateAsync(existing, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("DailyBriefing upsert: user={UserId} date={Date} source={Source} id={Id}", userId, today, source, existing.Id);
        return Result.Success(existing.Id);
    }

    public async Task<Result<IEnumerable<DailyBriefingCardResponse>>> GetTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var today = BrtToday();
        var items = await repo.GetByUserAndDateAsync(userId, today, ct);
        var cards = items.Select(b => new DailyBriefingCardResponse(b.Id, b.Source, b.Title, b.BriefingDate, b.CreatedAt));
        return Result.Success(cards);
    }

    public async Task<Result<DailyBriefingDetailResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAndUserAsync(id, userId, ct);
        if (b is null)
            return Result.Failure<DailyBriefingDetailResponse>(new Error("DailyBriefing.NotFound", "Briefing não encontrado."));

        return Result.Success(new DailyBriefingDetailResponse(b.Id, b.Source, b.Title, b.Content, b.ContentFormat, b.BriefingDate, b.CreatedAt));
    }

    private static TimeZoneInfo ResolveBrtTimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* try next */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
    }
}
