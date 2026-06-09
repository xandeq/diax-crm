namespace Diax.Application.Briefings.Dtos;

/// <summary>Payload de ingestão (vindo dos geradores via X-Integration-Key).</summary>
public record IngestDailyBriefingRequest(
    string Source,
    string Title,
    string Content,
    string? Format = null,
    DateOnly? Date = null);

/// <summary>Card exibido na lista (sem o conteúdo pesado).</summary>
public record DailyBriefingCardResponse(
    Guid Id,
    string Source,
    string Title,
    DateOnly Date,
    DateTime CreatedAt);

/// <summary>Detalhe completo (ao abrir um briefing).</summary>
public record DailyBriefingDetailResponse(
    Guid Id,
    string Source,
    string Title,
    string Content,
    string Format,
    DateOnly Date,
    DateTime CreatedAt);
