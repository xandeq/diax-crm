using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public interface IAppointmentLabelRepository : IRepository<AppointmentLabel>
{
    Task<IEnumerable<AppointmentLabel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
