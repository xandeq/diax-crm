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
            .Where(a => a.Date >= todayStart && a.Date <= todayEnd && !a.DailyNotificationSent && !a.IsCancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(x => x.Date >= startDate && x.Date <= endDate && !x.IsCancelled)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeWithLabelAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Include(x => x.Label)
            .Where(x => x.Date >= startDate && x.Date <= endDate && !x.IsCancelled)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Appointment?> GetByIdWithLabelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Include(x => x.Label)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByRecurrenceGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .IgnoreQueryFilters()
            .Where(x => x.RecurrenceGroupId == groupId)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }
}
