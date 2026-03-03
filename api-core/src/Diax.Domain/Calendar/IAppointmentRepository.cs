using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetPendingDailyNotificationAsync(DateTime todayStart, DateTime todayEnd, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
