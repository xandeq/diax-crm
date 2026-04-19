namespace Diax.Application.Calendar.Dtos;

public record AiBatchCommandDto
{
    public required string Command { get; init; }
    public required AppointmentSummaryForBatchDto[] Appointments { get; init; }
}

public record AppointmentSummaryForBatchDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Date { get; init; } // ISO 8601 UTC
    public string? LabelName { get; init; }
}

public record AiBatchChangeDto
{
    public required Guid Id { get; init; }
    public string? NewDate { get; init; }   // ISO 8601 UTC
    public string? NewTitle { get; init; }
    public bool Delete { get; init; }
}

public record AiBatchResponseDto
{
    public required string Summary { get; init; }
    public required AiBatchChangeDto[] Changes { get; init; }
}
