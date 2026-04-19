using Diax.Domain.Calendar;

namespace Diax.Application.Calendar.Dtos;

public record RecurringAppointmentDto
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public AppointmentType Type { get; init; }
    public Guid? LabelId { get; init; }
    public int DurationMinutes { get; init; } = 60;

    /// <summary>Hora e minutos no formato "HH:mm" (ex: "10:30")</summary>
    public required string TimeHHmm { get; init; }

    /// <summary>Dias da semana: 0=Dom, 1=Seg, 2=Ter, 3=Qua, 4=Qui, 5=Sex, 6=Sáb</summary>
    public required int[] DaysOfWeek { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>Datas específicas a excluir da série (formato "yyyy-MM-dd")</summary>
    public string[]? ExcludedDates { get; init; }
}
