using Diax.Application.Briefings.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Briefings;

public interface IDailyBriefingService
{
    /// <summary>Insere/atualiza o briefing do dia para um source e purga os de dias anteriores.</summary>
    Task<Result<Guid>> UpsertAsync(Guid userId, IngestDailyBriefingRequest request, CancellationToken ct = default);

    /// <summary>Cards do dia corrente (America/São_Paulo).</summary>
    Task<Result<IEnumerable<DailyBriefingCardResponse>>> GetTodayAsync(Guid userId, CancellationToken ct = default);

    Task<Result<DailyBriefingDetailResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
