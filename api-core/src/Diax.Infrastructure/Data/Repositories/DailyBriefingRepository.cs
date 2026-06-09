using Diax.Domain.Briefings;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class DailyBriefingRepository : Repository<DailyBriefing>, IDailyBriefingRepository
{
    private readonly DiaxDbContext _context;

    public DailyBriefingRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    // IgnoreQueryFilters + filtro explícito por userId: funciona tanto no caminho autenticado (JWT)
    // quanto no de integração (X-Integration-Key, sem CurrentUser → query filter esconderia tudo).

    public async Task<IEnumerable<DailyBriefing>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
        => await _context.DailyBriefings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.BriefingDate == date)
            .OrderBy(b => b.Source)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<DailyBriefing>> GetOtherDaysAsync(Guid userId, DateOnly keepDate, CancellationToken cancellationToken = default)
        => await _context.DailyBriefings
            .IgnoreQueryFilters()
            .Where(b => b.UserId == userId && b.BriefingDate != keepDate)
            .ToListAsync(cancellationToken);

    public async Task<DailyBriefing?> GetByUserDateSourceAsync(Guid userId, DateOnly date, string source, CancellationToken cancellationToken = default)
        => await _context.DailyBriefings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.BriefingDate == date && b.Source == source, cancellationToken);

    public async Task<DailyBriefing?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => await _context.DailyBriefings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

    public void RemoveRange(IEnumerable<DailyBriefing> briefings)
        => _context.DailyBriefings.RemoveRange(briefings);
}
