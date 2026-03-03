using Diax.Application.Calendar.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Calendar;

public interface IAppointmentService
{
    Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AppointmentDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Result<AppointmentDto>> UpdateAsync(Guid id, UpdateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> SendDailyAgendaNotificationAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CreateAppointmentDto>>> ParseFromTextAsync(string text, CancellationToken cancellationToken = default);
}
