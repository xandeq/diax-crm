using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Diax.Application.SurfAlerts.Dtos;

/// <summary>
/// Payload enviado por uma automação externa (ex: previsão semanal de surf cruzada
/// com agenda). Nomes de campo em snake_case porque o contrato é definido pelo emissor,
/// não segue a convenção camelCase do restante da API.
/// </summary>
public class IngestSurfAlertRequest
{
    [Required, MaxLength(100)]
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("generated_at")]
    public DateTimeOffset GeneratedAt { get; set; }

    [Required]
    [JsonPropertyName("weekend")]
    public WeekendDto Weekend { get; set; } = new();

    [JsonPropertyName("has_good_window")]
    public bool HasGoodWindow { get; set; }

    [Required, MaxLength(1000)]
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("best_window")]
    public BestWindowDto? BestWindow { get; set; }

    [JsonPropertyName("alternatives")]
    public List<AlternativeDto> Alternatives { get; set; } = [];

    [JsonPropertyName("calendar_conflicts")]
    public List<CalendarConflictDto> CalendarConflicts { get; set; } = [];

    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = [];
}

public class WeekendDto
{
    [Required]
    [JsonPropertyName("saturday")]
    public DateOnly Saturday { get; set; }

    [Required]
    [JsonPropertyName("sunday")]
    public DateOnly Sunday { get; set; }
}

public class WaveHeightDto
{
    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }
}

public class SwellDto
{
    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("period_s")]
    public double? PeriodS { get; set; }
}

public class WindDto
{
    [JsonPropertyName("speed_kmh")]
    public double? SpeedKmh { get; set; }

    [JsonPropertyName("gust_kmh")]
    public double? GustKmh { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class BestWindowDto
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("spot")]
    public string? Spot { get; set; }

    [JsonPropertyName("wave_height_m")]
    public WaveHeightDto? WaveHeightM { get; set; }

    [JsonPropertyName("swell")]
    public SwellDto? Swell { get; set; }

    [JsonPropertyName("wind")]
    public WindDto? Wind { get; set; }

    [JsonPropertyName("tide")]
    public string? Tide { get; set; }

    [JsonPropertyName("weather")]
    public string? Weather { get; set; }

    [Range(1, 5)]
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("calendar_free")]
    public bool? CalendarFree { get; set; }
}

public class AlternativeDto
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("spot")]
    public string? Spot { get; set; }

    [JsonPropertyName("wave_height_m")]
    public WaveHeightDto? WaveHeightM { get; set; }

    [JsonPropertyName("wind")]
    public WindDto? Wind { get; set; }

    [Range(1, 5)]
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("calendar_free")]
    public bool? CalendarFree { get; set; }
}

public class CalendarConflictDto
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
