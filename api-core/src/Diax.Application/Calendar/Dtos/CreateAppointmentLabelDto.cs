namespace Diax.Application.Calendar.Dtos;

public record CreateAppointmentLabelDto
{
    public required string Name { get; init; }
    public required string Color { get; init; }
    public int Order { get; init; }
}
