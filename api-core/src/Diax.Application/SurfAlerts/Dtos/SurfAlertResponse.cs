using System.Text.Json;
using Diax.Domain.SurfAlerts;

namespace Diax.Application.SurfAlerts.Dtos;

public class SurfAlertResponse
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateOnly WeekendSaturday { get; set; }
    public DateOnly WeekendSunday { get; set; }
    public bool HasGoodWindow { get; set; }
    public string Summary { get; set; } = string.Empty;
    public JsonElement? BestWindow { get; set; }
    public JsonElement? Alternatives { get; set; }
    public JsonElement? CalendarConflicts { get; set; }
    public JsonElement? Sources { get; set; }
    public DateTime CreatedAt { get; set; }

    public static SurfAlertResponse FromEntity(SurfAlert alert) => new()
    {
        Id = alert.Id,
        Source = alert.Source,
        Type = alert.Type,
        GeneratedAt = alert.GeneratedAt,
        WeekendSaturday = alert.WeekendSaturday,
        WeekendSunday = alert.WeekendSunday,
        HasGoodWindow = alert.HasGoodWindow,
        Summary = alert.Summary,
        BestWindow = ParseOrNull(alert.BestWindowJson),
        Alternatives = ParseOrNull(alert.AlternativesJson),
        CalendarConflicts = ParseOrNull(alert.CalendarConflictsJson),
        Sources = ParseOrNull(alert.SourcesJson),
        CreatedAt = alert.CreatedAt
    };

    private static JsonElement? ParseOrNull(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json).RootElement;
}
