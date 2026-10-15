using Diax.Domain.Common;

namespace Diax.Domain.SurfAlerts;

/// <summary>
/// Alerta de janela de surf recebido de uma automação externa (ex: previsão semanal
/// cruzada com agenda). Os blocos variáveis do payload (janela ideal, alternativas,
/// conflitos de agenda, fontes) são guardados como JSON — o formato é definido pelo
/// emissor, não pelo CRM.
/// </summary>
public class SurfAlert : Entity
{
    public string Source { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }
    public DateOnly WeekendSaturday { get; private set; }
    public DateOnly WeekendSunday { get; private set; }
    public bool HasGoodWindow { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? BestWindowJson { get; private set; }
    public string? AlternativesJson { get; private set; }
    public string? CalendarConflictsJson { get; private set; }
    public string? SourcesJson { get; private set; }
    public string RawPayloadJson { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected SurfAlert() { }

    private SurfAlert(
        string source,
        string type,
        DateTime generatedAt,
        DateOnly weekendSaturday,
        DateOnly weekendSunday,
        bool hasGoodWindow,
        string summary,
        string? bestWindowJson,
        string? alternativesJson,
        string? calendarConflictsJson,
        string? sourcesJson,
        string rawPayloadJson,
        string? idempotencyKey)
    {
        Source = source;
        Type = type;
        GeneratedAt = generatedAt;
        WeekendSaturday = weekendSaturday;
        WeekendSunday = weekendSunday;
        HasGoodWindow = hasGoodWindow;
        Summary = summary;
        BestWindowJson = bestWindowJson;
        AlternativesJson = alternativesJson;
        CalendarConflictsJson = calendarConflictsJson;
        SourcesJson = sourcesJson;
        RawPayloadJson = rawPayloadJson;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
    }

    public static SurfAlert Create(
        string source,
        string type,
        DateTime generatedAt,
        DateOnly weekendSaturday,
        DateOnly weekendSunday,
        bool hasGoodWindow,
        string summary,
        string? bestWindowJson,
        string? alternativesJson,
        string? calendarConflictsJson,
        string? sourcesJson,
        string rawPayloadJson,
        string? idempotencyKey)
        => new(source, type, generatedAt, weekendSaturday, weekendSunday, hasGoodWindow,
            summary, bestWindowJson, alternativesJson, calendarConflictsJson, sourcesJson,
            rawPayloadJson, idempotencyKey);
}
