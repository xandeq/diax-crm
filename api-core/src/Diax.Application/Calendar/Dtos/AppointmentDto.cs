using Diax.Domain.Calendar;

namespace Diax.Application.Calendar.Dtos;

public record AppointmentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime Date { get; init; }
    public AppointmentType Type { get; init; }
    public bool DailyNotificationSent { get; init; }
}
