using Diax.Domain.Calendar;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AppointmentLabelRepository : Repository<AppointmentLabel>, IAppointmentLabelRepository
{
    private readonly DiaxDbContext _context;

    public AppointmentLabelRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppointmentLabel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AppointmentLabels
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }
}
