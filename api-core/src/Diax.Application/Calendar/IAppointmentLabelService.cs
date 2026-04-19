using Diax.Application.Calendar.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Calendar;

public interface IAppointmentLabelService
{
    Task<Result<IEnumerable<AppointmentLabelDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AppointmentLabelDto>> CreateAsync(CreateAppointmentLabelDto dto, CancellationToken cancellationToken = default);
    Task<Result<AppointmentLabelDto>> UpdateAsync(Guid id, CreateAppointmentLabelDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
