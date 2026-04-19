using Diax.Domain.Calendar;

namespace Diax.Application.Calendar.Dtos;

public record UpdateAppointmentDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? Date { get; init; }
    public AppointmentType? Type { get; init; }
    public int? DurationMinutes { get; init; }
    public Guid? LabelId { get; init; }
}
