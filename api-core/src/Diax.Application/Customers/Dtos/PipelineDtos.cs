using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

public record PipelineCardDto(
    Guid Id,
    string Name,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? WhatsApp,
    decimal? EstimatedValue,
    DateTime? ExpectedCloseDate,
    int? LeadScore,
    string? Segment,
    DateTime? LastContactAt,
    string? Tags
);

public record PipelineColumnDto(
    CustomerStatus Status,
    string Label,
    decimal Probability,   // 0..1 — peso do estágio na previsão ponderada
    int Count,
    decimal TotalValue,
    List<PipelineCardDto> Cards
);

public record PipelineBoardDto(
    List<PipelineColumnDto> Columns,
    int TotalOpenDeals,
    decimal TotalOpenValue,
    decimal WeightedForecast,
    decimal WonLast30DaysValue,
    int WonLast30DaysCount
);

public record MovePipelineStageRequest(CustomerStatus Status);

public record UpdatePipelineDealRequest(decimal? EstimatedValue, DateTime? ExpectedCloseDate);
