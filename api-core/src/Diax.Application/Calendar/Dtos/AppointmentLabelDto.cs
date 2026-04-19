namespace Diax.Application.Calendar.Dtos;

public record AppointmentLabelDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = "#64748b";
    public int Order { get; init; }
}
