using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetPendingDailyNotificationAsync(DateTime todayStart, DateTime todayEnd, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByDateRangeWithLabelAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Appointment?> GetByIdWithLabelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByRecurrenceGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
