using Diax.Domain.Calendar;

namespace Diax.Application.Calendar.Dtos;

public record CreateAppointmentDto
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required DateTime Date { get; init; }
    public AppointmentType Type { get; init; }
}
