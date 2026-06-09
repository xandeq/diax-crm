using Diax.Domain.Common;

namespace Diax.Domain.Briefings;

public interface IDailyBriefingRepository : IRepository<DailyBriefing>
{
    /// <summary>Briefings de um dia específico (ordenados por source). Ignora query filter (uso por integração anônima).</summary>
    Task<IEnumerable<DailyBriefing>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Briefings do usuário de dias diferentes de <paramref name="keepDate"/> (para purga).</summary>
    Task<IEnumerable<DailyBriefing>> GetOtherDaysAsync(Guid userId, DateOnly keepDate, CancellationToken cancellationToken = default);

    Task<DailyBriefing?> GetByUserDateSourceAsync(Guid userId, DateOnly date, string source, CancellationToken cancellationToken = default);

    Task<DailyBriefing?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<DailyBriefing> briefings);
}
