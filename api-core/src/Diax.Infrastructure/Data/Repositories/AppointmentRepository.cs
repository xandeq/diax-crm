using Diax.Domain.Calendar;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    private readonly DiaxDbContext _context;

    public AppointmentRepository(DiaxDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Appointment>> GetPendingDailyNotificationAsync(DateTime todayStart, DateTime todayEnd, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.Date >= todayStart && a.Date <= todayEnd && !a.DailyNotificationSent)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(x => x.Date >= startDate && x.Date <= endDate)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }
}
